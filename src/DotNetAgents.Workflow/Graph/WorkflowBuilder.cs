using DotNetAgents.Abstractions.Chains;
using DotNetAgents.Abstractions.Exceptions;

namespace DotNetAgents.Workflow.Graph;

/// <summary>
/// Fluent builder for creating workflows (state graphs).
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public class WorkflowBuilder<TState> where TState : class
{
    private readonly StateGraph<TState> _graph;
    private string? _entryPoint;
    private readonly List<string> _exitPoints = new();

    /// <summary>
    /// Creates a new workflow builder.
    /// </summary>
    /// <typeparam name="TState">The type of the workflow state.</typeparam>
    /// <returns>A new workflow builder instance.</returns>
    public static WorkflowBuilder<TState> Create()
    {
        return new WorkflowBuilder<TState>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowBuilder{TState}"/> class.
    /// </summary>
    public WorkflowBuilder()
    {
        _graph = new StateGraph<TState>();
    }

    /// <summary>
    /// Adds a node to the workflow.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="handler">The handler function for the node.</param>
    /// <returns>The workflow builder for method chaining.</returns>
    public WorkflowBuilder<TState> AddNode(
        string name,
        Func<TState, CancellationToken, Task<TState>> handler)
    {
        _graph.AddNode(name, handler);
        return this;
    }

    /// <summary>
    /// Adds a node using an IRunnable.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="runnable">The runnable to execute.</param>
    /// <returns>The workflow builder for method chaining.</returns>
    public WorkflowBuilder<TState> AddNode(
        string name,
        IRunnable<TState, TState> runnable)
    {
        if (runnable == null)
            throw new ArgumentNullException(nameof(runnable));

        // Create a handler that wraps the runnable
        _graph.AddNode(name, async (state, ct) =>
        {
            return await runnable.InvokeAsync(state, null, ct).ConfigureAwait(false);
        });
        return this;
    }

    /// <summary>
    /// Adds an edge between two nodes.
    /// </summary>
    /// <param name="from">The source node name.</param>
    /// <param name="to">The target node name.</param>
    /// <param name="condition">Optional condition function.</param>
    /// <returns>The workflow builder for method chaining.</returns>
    public WorkflowBuilder<TState> AddEdge(
        string from,
        string to,
        Func<TState, bool>? condition = null)
    {
        _graph.AddEdge(from, to, condition);
        return this;
    }

    /// <summary>
    /// Sets the entry point of the workflow.
    /// </summary>
    /// <param name="nodeName">The name of the entry node.</param>
    /// <returns>The workflow builder for method chaining.</returns>
    public WorkflowBuilder<TState> SetEntryPoint(string nodeName)
    {
        _entryPoint = nodeName;
        _graph.SetEntryPoint(nodeName);
        return this;
    }

    /// <summary>
    /// Adds an exit point to the workflow.
    /// </summary>
    /// <param name="nodeName">The name of the exit node.</param>
    /// <returns>The workflow builder for method chaining.</returns>
    public WorkflowBuilder<TState> AddExitPoint(string nodeName)
    {
        if (!_exitPoints.Contains(nodeName))
        {
            _exitPoints.Add(nodeName);
        }

        _graph.AddExitPoint(nodeName);
        return this;
    }

    /// <summary>
    /// Builds the workflow graph.
    /// </summary>
    /// <returns>The built state graph.</returns>
    /// <exception cref="AgentException">Thrown when the workflow is invalid.</exception>
    public StateGraph<TState> Build()
    {
        if (string.IsNullOrWhiteSpace(_entryPoint))
        {
            throw new AgentException(
                "Workflow must have an entry point.",
                ErrorCategory.ConfigurationError);
        }

        _graph.Validate();
        return _graph;
    }
}
