// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Agents.Tasks;
using DotNetAgents.Workflow.Graph;
using Microsoft.Extensions.Logging;
using TaskStatus = DotNetAgents.Agents.Tasks.TaskStatus;

namespace DotNetAgents.Workflow.MultiAgent;

/// <summary>
/// A workflow node that aggregates results from completed worker tasks.
/// </summary>
/// <typeparam name="TState">The workflow state type. Must extend <see cref="MultiAgentWorkflowState"/>.</typeparam>
public class AggregateResultsNode<TState> : GraphNode<TState> where TState : MultiAgentWorkflowState
{
    private readonly IWorkerDelegationSink _supervisor;
    private readonly Func<TState, Dictionary<string, WorkerTaskResult>, TState> _aggregator;
    private readonly ILogger<AggregateResultsNode<TState>>? _logger;
    private readonly bool _waitForAllTasks;

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateResultsNode{TState}"/> class.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="supervisor">The supervisor agent for retrieving task results.</param>
    /// <param name="aggregator">A function that aggregates results into the state.</param>
    /// <param name="waitForAllTasks">If true, waits for all pending tasks to complete before aggregating. If false, aggregates only completed tasks.</param>
    /// <param name="logger">Optional logger instance.</param>
    public AggregateResultsNode(
        string name,
        IWorkerDelegationSink supervisor,
        Func<TState, Dictionary<string, WorkerTaskResult>, TState> aggregator,
        bool waitForAllTasks = true,
        ILogger<AggregateResultsNode<TState>>? logger = null)
        : base(name, CreateHandler(supervisor, aggregator, waitForAllTasks, logger, name))
    {
        _supervisor = supervisor ?? throw new ArgumentNullException(nameof(supervisor));
        _aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));
        _waitForAllTasks = waitForAllTasks;
        _logger = logger;
        Description = "Aggregates results from worker tasks";
    }

    private static Func<TState, CancellationToken, Task<TState>> CreateHandler(
        IWorkerDelegationSink supervisor,
        Func<TState, Dictionary<string, WorkerTaskResult>, TState> aggregator,
        bool waitForAllTasks,
        ILogger<AggregateResultsNode<TState>>? logger,
        string nodeName)
    {
        return async (state, ct) =>
        {
            ArgumentNullException.ThrowIfNull(state);
            ct.ThrowIfCancellationRequested();

            logger?.LogInformation("Node {NodeName}: Starting result aggregation", nodeName);

            if (waitForAllTasks && state.PendingTaskIds.Count > 0)
            {
                logger?.LogInformation(
                    "Node {NodeName}: Waiting for {PendingCount} pending tasks to complete",
                    nodeName,
                    state.PendingTaskIds.Count);

                await WaitForTasksAsync(state, supervisor, ct, logger, nodeName).ConfigureAwait(false);
            }

            var completedResults = new Dictionary<string, WorkerTaskResult>();

            foreach (var taskId in state.CompletedTaskIds)
            {
                var result = await supervisor.GetTaskResultAsync(taskId, ct).ConfigureAwait(false);
                if (result != null)
                {
                    completedResults[taskId] = result;
                }
            }

            foreach (var taskId in state.PendingTaskIds.ToList())
            {
                var status = await supervisor.GetTaskStatusAsync(taskId, ct).ConfigureAwait(false);
                if (status == TaskStatus.Completed)
                {
                    var result = await supervisor.GetTaskResultAsync(taskId, ct).ConfigureAwait(false);
                    if (result != null)
                    {
                        completedResults[taskId] = result;
                        state.PendingTaskIds.Remove(taskId);
                        state.CompletedTaskIds.Add(taskId);
                    }
                }
                else if (status == TaskStatus.Failed)
                {
                    var result = await supervisor.GetTaskResultAsync(taskId, ct).ConfigureAwait(false);
                    if (result != null)
                    {
                        completedResults[taskId] = result;
                        state.PendingTaskIds.Remove(taskId);
                        state.FailedTaskIds.Add(taskId);
                    }
                }
            }

            foreach (var result in completedResults.Values)
            {
                state.TaskResults[result.TaskId] = result;
            }

            logger?.LogInformation(
                "Node {NodeName}: Aggregated {ResultCount} task results",
                nodeName,
                completedResults.Count);

            return aggregator(state, completedResults);
        };
    }

    private static async Task WaitForTasksAsync(
        TState state,
        IWorkerDelegationSink supervisor,
        CancellationToken cancellationToken,
        ILogger<AggregateResultsNode<TState>>? logger,
        string nodeName)
    {
        const int maxWaitTimeMs = 300000; // 5 minutes max
        const int pollIntervalMs = 1000; // Poll every second
        var startTime = DateTimeOffset.UtcNow;

        while (state.PendingTaskIds.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if ((DateTimeOffset.UtcNow - startTime).TotalMilliseconds > maxWaitTimeMs)
            {
                logger?.LogWarning(
                    "Node {NodeName}: Timeout waiting for tasks. {PendingCount} tasks still pending",
                    nodeName,
                    state.PendingTaskIds.Count);
                break;
            }

            // Check status of pending tasks
            var completedIds = new List<string>();
            var failedIds = new List<string>();

            foreach (var taskId in state.PendingTaskIds)
            {
                var status = await supervisor.GetTaskStatusAsync(taskId, cancellationToken).ConfigureAwait(false);
                if (status == TaskStatus.Completed)
                {
                    completedIds.Add(taskId);
                }
                else if (status == TaskStatus.Failed)
                {
                    failedIds.Add(taskId);
                }
            }

            // Update state
            foreach (var taskId in completedIds)
            {
                state.PendingTaskIds.Remove(taskId);
                state.CompletedTaskIds.Add(taskId);
            }

            foreach (var taskId in failedIds)
            {
                state.PendingTaskIds.Remove(taskId);
                state.FailedTaskIds.Add(taskId);
            }

            if (state.PendingTaskIds.Count > 0)
            {
                await Task.Delay(pollIntervalMs, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
