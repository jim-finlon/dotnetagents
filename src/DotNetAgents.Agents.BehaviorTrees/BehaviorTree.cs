using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// Represents a behavior tree with a root node.
/// </summary>
/// <typeparam name="TContext">The type of the execution context.</typeparam>
public class BehaviorTree<TContext> where TContext : class
{
    /// <summary>
    /// Gets the root node of the behavior tree.
    /// </summary>
    public IBehaviorTreeNode<TContext> Root { get; }

    /// <summary>
    /// Gets the name of the behavior tree.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets an optional description of the behavior tree.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BehaviorTree{TContext}"/> class.
    /// </summary>
    /// <param name="name">The name of the behavior tree.</param>
    /// <param name="root">The root node.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> or <paramref name="root"/> is null.</exception>
    public BehaviorTree(string name, IBehaviorTreeNode<TContext> root)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Root = root ?? throw new ArgumentNullException(nameof(root));
    }

    /// <summary>
    /// Executes the behavior tree.
    /// </summary>
    /// <param name="context">The execution context.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The execution status.</returns>
    public Task<BehaviorTreeNodeStatus> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Root.ExecuteAsync(context, cancellationToken);
    }

    /// <summary>
    /// Resets the behavior tree state.
    /// </summary>
    public void Reset()
    {
        Root.Reset();
    }
}
