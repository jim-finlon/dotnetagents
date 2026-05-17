using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// A composite node that executes children sequentially until one fails.
/// Returns Success if all children succeed, Failure if any child fails, Running if a child is running.
/// </summary>
/// <typeparam name="TContext">The type of the execution context.</typeparam>
public class SequenceNode<TContext> : CompositeNode<TContext> where TContext : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SequenceNode{TContext}"/> class.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="logger">Optional logger instance.</param>
    public SequenceNode(string name, ILogger<BehaviorTreeNode<TContext>>? logger = null)
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
            Logger?.LogWarning("Sequence node '{NodeName}' has no children", Name);
            return BehaviorTreeNodeStatus.Success;
        }

        Logger?.LogDebug("Executing sequence node '{NodeName}' with {ChildCount} children", Name, Children.Count);

        foreach (var child in Children)
        {
            var status = await child.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);

            if (status == BehaviorTreeNodeStatus.Failure)
            {
                Logger?.LogDebug("Sequence node '{NodeName}' failed at child '{ChildName}'", Name, child.Name);
                return BehaviorTreeNodeStatus.Failure;
            }

            if (status == BehaviorTreeNodeStatus.Running)
            {
                Logger?.LogDebug("Sequence node '{NodeName}' is running at child '{ChildName}'", Name, child.Name);
                return BehaviorTreeNodeStatus.Running;
            }
        }

        Logger?.LogDebug("Sequence node '{NodeName}' completed successfully", Name);
        return BehaviorTreeNodeStatus.Success;
    }
}
