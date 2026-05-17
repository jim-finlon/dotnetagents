using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// A composite node that executes children sequentially until one succeeds.
/// Returns Success if any child succeeds, Failure if all children fail, Running if a child is running.
/// </summary>
/// <typeparam name="TContext">The type of the execution context.</typeparam>
public class SelectorNode<TContext> : CompositeNode<TContext> where TContext : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SelectorNode{TContext}"/> class.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="logger">Optional logger instance.</param>
    public SelectorNode(string name, ILogger<BehaviorTreeNode<TContext>>? logger = null)
        : base(name, logger)
    {
    }

    /// <inheritdoc/>
    public override async Task<BehaviorTreeNodeStatus> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (Children.Count == 0)
        {
            Logger?.LogWarning("Selector node '{NodeName}' has no children", Name);
            return BehaviorTreeNodeStatus.Failure;
        }

        Logger?.LogDebug("Executing selector node '{NodeName}' with {ChildCount} children", Name, Children.Count);

        foreach (var child in Children)
        {
            var status = await child.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);

            if (status == BehaviorTreeNodeStatus.Success)
            {
                Logger?.LogDebug("Selector node '{NodeName}' succeeded at child '{ChildName}'", Name, child.Name);
                return BehaviorTreeNodeStatus.Success;
            }

            if (status == BehaviorTreeNodeStatus.Running)
            {
                Logger?.LogDebug("Selector node '{NodeName}' is running at child '{ChildName}'", Name, child.Name);
                return BehaviorTreeNodeStatus.Running;
            }
        }

        Logger?.LogDebug("Selector node '{NodeName}' failed - all children failed", Name);
        return BehaviorTreeNodeStatus.Failure;
    }
}
