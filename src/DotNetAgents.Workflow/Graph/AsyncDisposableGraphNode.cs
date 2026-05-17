namespace DotNetAgents.Workflow.Graph;

/// <summary>
/// Base class for graph nodes that require async disposal.
/// This is a .NET 10 optimization pattern for better async resource management.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public abstract class AsyncDisposableGraphNode<TState> : GraphNode<TState>, IAsyncDisposable
    where TState : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncDisposableGraphNode{TState}"/> class.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="handler">The handler function.</param>
    protected AsyncDisposableGraphNode(string name, Func<TState, CancellationToken, Task<TState>> handler)
        : base(name, handler)
    {
    }

    /// <summary>
    /// Performs async cleanup operations.
    /// </summary>
    /// <returns>A task representing the async disposal operation.</returns>
    public virtual ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
