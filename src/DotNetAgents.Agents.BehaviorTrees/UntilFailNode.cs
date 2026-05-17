using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// A decorator node that repeats its child until it fails.
/// Returns Success when the child fails, Running if the child is running.
/// </summary>
/// <typeparam name="TContext">The type of the execution context.</typeparam>
public class UntilFailNode<TContext> : DecoratorNode<TContext> where TContext : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UntilFailNode{TContext}"/> class.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="logger">Optional logger instance.</param>
    public UntilFailNode(string name, ILogger<BehaviorTreeNode<TContext>>? logger = null)
        : base(name, logger)
    {
    }

    /// <inheritdoc/>
    public override async Task<BehaviorTreeNodeStatus> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (Child == null)
        {
            Logger?.LogWarning("UntilFail node '{NodeName}' has no child", Name);
            return BehaviorTreeNodeStatus.Failure;
        }

        Logger?.LogDebug("Executing UntilFail node '{NodeName}'", Name);

        while (true)
        {
            var status = await Child.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);

            if (status == BehaviorTreeNodeStatus.Failure)
            {
                Logger?.LogDebug("UntilFail node '{NodeName}' succeeded (child failed)", Name);
                return BehaviorTreeNodeStatus.Success;
            }

            if (status == BehaviorTreeNodeStatus.Running)
            {
                Logger?.LogDebug("UntilFail node '{NodeName}' is running", Name);
                return BehaviorTreeNodeStatus.Running;
            }

            // Child succeeded, reset and repeat
            Child.Reset();
        }
    }
}
