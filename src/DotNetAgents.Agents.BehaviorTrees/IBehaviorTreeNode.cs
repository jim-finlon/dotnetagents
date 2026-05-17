namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// Represents a node in a behavior tree.
/// </summary>
/// <typeparam name="TContext">The type of the execution context.</typeparam>
public interface IBehaviorTreeNode<TContext> where TContext : class
{
    /// <summary>
    /// Gets the name of the node.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the node asynchronously.
    /// </summary>
    /// <param name="context">The execution context.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The execution status of the node.</returns>
    Task<BehaviorTreeNodeStatus> ExecuteAsync(TContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the node's internal state (for stateful nodes).
    /// </summary>
    void Reset();
}
