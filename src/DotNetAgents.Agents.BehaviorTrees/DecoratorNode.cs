using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// Base class for decorator nodes that modify the behavior of a single child node.
/// </summary>
/// <typeparam name="TContext">The type of the execution context.</typeparam>
public abstract class DecoratorNode<TContext> : BehaviorTreeNode<TContext> where TContext : class
{
    /// <summary>
    /// Gets the child node.
    /// </summary>
    protected IBehaviorTreeNode<TContext>? Child { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DecoratorNode{TContext}"/> class.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="logger">Optional logger instance.</param>
    protected DecoratorNode(string name, ILogger<BehaviorTreeNode<TContext>>? logger = null)
        : base(name, logger)
    {
    }

    /// <summary>
    /// Sets the child node.
    /// </summary>
    /// <param name="child">The child node.</param>
    /// <returns>The decorator node for method chaining.</returns>
    public DecoratorNode<TContext> SetChild(IBehaviorTreeNode<TContext> child)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        return this;
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        base.Reset();
        Child?.Reset();
    }
}
