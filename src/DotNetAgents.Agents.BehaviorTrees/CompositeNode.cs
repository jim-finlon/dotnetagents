using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// Base class for composite nodes that have child nodes.
/// </summary>
/// <typeparam name="TContext">The type of the execution context.</typeparam>
public abstract class CompositeNode<TContext> : BehaviorTreeNode<TContext> where TContext : class
{
    /// <summary>
    /// Gets the child nodes.
    /// </summary>
    protected List<IBehaviorTreeNode<TContext>> Children { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeNode{TContext}"/> class.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="logger">Optional logger instance.</param>
    protected CompositeNode(string name, ILogger<BehaviorTreeNode<TContext>>? logger = null)
        : base(name, logger)
    {
        Children = new List<IBehaviorTreeNode<TContext>>();
    }

    /// <summary>
    /// Adds a child node.
    /// </summary>
    /// <param name="child">The child node to add.</param>
    /// <returns>The composite node for method chaining.</returns>
    public CompositeNode<TContext> AddChild(IBehaviorTreeNode<TContext> child)
    {
        ArgumentNullException.ThrowIfNull(child);
        Children.Add(child);
        return this;
    }

    /// <summary>
    /// Adds multiple child nodes.
    /// </summary>
    /// <param name="children">The child nodes to add.</param>
    /// <returns>The composite node for method chaining.</returns>
    public CompositeNode<TContext> AddChildren(params IBehaviorTreeNode<TContext>[] children)
    {
        ArgumentNullException.ThrowIfNull(children);
        foreach (var child in children)
        {
            if (child != null)
            {
                Children.Add(child);
            }
        }
        return this;
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        base.Reset();
        foreach (var child in Children)
        {
            child.Reset();
        }
    }
}
