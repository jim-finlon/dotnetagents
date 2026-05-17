using DotNetAgents.Abstractions.Chains;

namespace DotNetAgents.Workflow.Graph;

/// <summary>
/// Represents a node in a workflow graph.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public class GraphNode<TState> where TState : class
{
    /// <summary>
    /// Gets the unique name of the node.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the handler function that processes the state.
    /// </summary>
    public Func<TState, CancellationToken, Task<TState>> Handler { get; }

    /// <summary>
    /// Gets or sets an optional runnable that can be executed by this node.
    /// </summary>
    public IRunnable<TState, TState>? Runnable { get; set; }

    /// <summary>
    /// Gets or sets a description of what this node does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphNode{TState}"/> class.
    /// </summary>
    /// <param name="name">The unique name of the node.</param>
    /// <param name="handler">The handler function that processes the state.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> or <paramref name="handler"/> is null.</exception>
    public GraphNode(string name, Func<TState, CancellationToken, Task<TState>> handler)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphNode{TState}"/> class with a runnable.
    /// </summary>
    /// <param name="name">The unique name of the node.</param>
    /// <param name="runnable">The runnable to execute.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> or <paramref name="runnable"/> is null.</exception>
    public GraphNode(string name, IRunnable<TState, TState> runnable)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Runnable = runnable ?? throw new ArgumentNullException(nameof(runnable));
        Handler = async (state, ct) =>
        {
            var result = await runnable.InvokeAsync(state, cancellationToken: ct).ConfigureAwait(false);
            return result;
        };
    }

    /// <summary>
    /// Executes the node with the given state.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The updated state after node execution.</returns>
    public async Task<TState> ExecuteAsync(TState state, CancellationToken cancellationToken = default)
    {
        return await Handler(state, cancellationToken).ConfigureAwait(false);
    }
}
