using DotNetAgents.Agents.Tasks;
using DotNetAgents.Workflow.Graph;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Workflow.MultiAgent;

/// <summary>
/// A workflow node that delegates tasks to worker agents via a supervisor.
/// </summary>
/// <typeparam name="TState">The workflow state type. Must extend <see cref="MultiAgentWorkflowState"/>.</typeparam>
public class DelegateToWorkerNode<TState> : GraphNode<TState> where TState : MultiAgentWorkflowState
{
    private readonly IWorkerDelegationSink _supervisor;
    private readonly Func<TState, IEnumerable<WorkerTask>> _taskFactory;
    private readonly ILogger<DelegateToWorkerNode<TState>>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelegateToWorkerNode{TState}"/> class.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="supervisor">The supervisor agent for delegating tasks.</param>
    /// <param name="taskFactory">A function that creates tasks from the workflow state.</param>
    /// <param name="logger">Optional logger instance.</param>
    public DelegateToWorkerNode(
        string name,
        IWorkerDelegationSink supervisor,
        Func<TState, IEnumerable<WorkerTask>> taskFactory,
        ILogger<DelegateToWorkerNode<TState>>? logger = null)
        : base(name, CreateHandler(supervisor, taskFactory, logger, name))
    {
        _supervisor = supervisor ?? throw new ArgumentNullException(nameof(supervisor));
        _taskFactory = taskFactory ?? throw new ArgumentNullException(nameof(taskFactory));
        _logger = logger;
        Description = "Delegates tasks to worker agents";
    }

    private static Func<TState, CancellationToken, Task<TState>> CreateHandler(
        IWorkerDelegationSink supervisor,
        Func<TState, IEnumerable<WorkerTask>> taskFactory,
        ILogger<DelegateToWorkerNode<TState>>? logger,
        string nodeName)
    {
        return async (state, ct) =>
        {
            ArgumentNullException.ThrowIfNull(state);
            ct.ThrowIfCancellationRequested();

            logger?.LogInformation("Node {NodeName}: Creating tasks from state", nodeName);

            var tasks = taskFactory(state).ToList();
            if (tasks.Count == 0)
            {
                logger?.LogWarning("Node {NodeName}: No tasks created from state", nodeName);
                return state;
            }

            logger?.LogInformation("Node {NodeName}: Submitting {TaskCount} tasks to supervisor", nodeName, tasks.Count);

            var taskIds = await supervisor.SubmitTasksAsync(tasks, ct).ConfigureAwait(false);

            state.SubmittedTasks.AddRange(tasks);
            state.PendingTaskIds.AddRange(taskIds);

            logger?.LogInformation(
                "Node {NodeName}: Successfully submitted {TaskCount} tasks. Task IDs: {TaskIds}",
                nodeName,
                taskIds.Count,
                string.Join(", ", taskIds));

            return state;
        };
    }

    private async Task<TState> ProcessAsync(TState state, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        cancellationToken.ThrowIfCancellationRequested();

        _logger?.LogInformation("Node {NodeName}: Creating tasks from state", Name);

        // Create tasks from state
        var tasks = _taskFactory(state).ToList();
        if (tasks.Count == 0)
        {
            _logger?.LogWarning("Node {NodeName}: No tasks created from state", Name);
            return state;
        }

        _logger?.LogInformation("Node {NodeName}: Submitting {TaskCount} tasks to supervisor", Name, tasks.Count);

        // Submit all tasks to supervisor
        var taskIds = await _supervisor.SubmitTasksAsync(tasks, cancellationToken).ConfigureAwait(false);

        // Update state with submitted tasks
        state.SubmittedTasks.AddRange(tasks);
        state.PendingTaskIds.AddRange(taskIds);

        _logger?.LogInformation(
            "Node {NodeName}: Successfully submitted {TaskCount} tasks. Task IDs: {TaskIds}",
            Name,
            taskIds.Count,
            string.Join(", ", taskIds));

        return state;
    }
}
