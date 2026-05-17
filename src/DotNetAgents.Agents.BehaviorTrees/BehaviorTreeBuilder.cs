using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// Fluent builder for constructing behavior trees.
/// </summary>
/// <typeparam name="TContext">The type of the execution context.</typeparam>
public class BehaviorTreeBuilder<TContext> where TContext : class
{
    private readonly ILogger<BehaviorTreeNode<TContext>>? _logger;
    private IBehaviorTreeNode<TContext>? _root;

    /// <summary>
    /// Initializes a new instance of the <see cref="BehaviorTreeBuilder{TContext}"/> class.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    public BehaviorTreeBuilder(ILogger<BehaviorTreeNode<TContext>>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Sets the root node of the behavior tree.
    /// </summary>
    /// <param name="root">The root node.</param>
    /// <returns>The builder for method chaining.</returns>
    public BehaviorTreeBuilder<TContext> SetRoot(IBehaviorTreeNode<TContext> root)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
        return this;
    }

    /// <summary>
    /// Builds the behavior tree.
    /// </summary>
    /// <param name="name">The name of the behavior tree.</param>
    /// <returns>The constructed behavior tree.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no root node has been set.</exception>
    public BehaviorTree<TContext> Build(string name)
    {
        if (_root == null)
        {
            throw new InvalidOperationException("Root node must be set before building the behavior tree.");
        }

        return new BehaviorTree<TContext>(name, _root);
    }
}
