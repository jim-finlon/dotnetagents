using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// Base class for behavior tree nodes with common functionality.
/// </summary>
/// <typeparam name="TContext">The type of the execution context.</typeparam>
public abstract class BehaviorTreeNode<TContext> : IBehaviorTreeNode<TContext> where TContext : class
{
    /// <inheritdoc/>
    public string Name { get; }

    /// <summary>
    /// Gets or sets an optional description of what this node does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger<BehaviorTreeNode<TContext>>? Logger { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BehaviorTreeNode{TContext}"/> class.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="logger">Optional logger instance.</param>
    protected BehaviorTreeNode(string name, ILogger<BehaviorTreeNode<TContext>>? logger = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Logger = logger;
    }

    /// <inheritdoc/>
    public abstract Task<BehaviorTreeNodeStatus> ExecuteAsync(TContext context, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public virtual void Reset()
    {
        // Default implementation does nothing - override for stateful nodes
    }
}
