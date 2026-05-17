using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// A decorator node that only executes its child if a condition is met.
/// Returns Success if the condition is false (skipping the child), otherwise returns the child's status.
/// </summary>
/// <typeparam name="TContext">The type of the execution context.</typeparam>
public class ConditionalDecoratorNode<TContext> : DecoratorNode<TContext> where TContext : class
{
    private readonly Func<TContext, bool> _condition;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConditionalDecoratorNode{TContext}"/> class.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="condition">The condition to check before executing the child.</param>
    /// <param name="logger">Optional logger instance.</param>
    public ConditionalDecoratorNode(
        string name,
        Func<TContext, bool> condition,
        ILogger<BehaviorTreeNode<TContext>>? logger = null)
        : base(name, logger)
    {
        _condition = condition ?? throw new ArgumentNullException(nameof(condition));
    }

    /// <inheritdoc/>
    public override async Task<BehaviorTreeNodeStatus> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (Child == null)
        {
            Logger?.LogWarning("Conditional decorator node '{NodeName}' has no child", Name);
            return BehaviorTreeNodeStatus.Failure;
        }

        Logger?.LogDebug("Evaluating condition for conditional decorator node '{NodeName}'", Name);

        try
        {
            var conditionMet = _condition(context);

            if (!conditionMet)
            {
                Logger?.LogDebug("Conditional decorator node '{NodeName}' skipped child (condition false)", Name);
                return BehaviorTreeNodeStatus.Success;
            }

            Logger?.LogDebug("Conditional decorator node '{NodeName}' executing child (condition true)", Name);
            var status = await Child.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
            Logger?.LogDebug("Conditional decorator node '{NodeName}' completed with status '{Status}'", Name, status);
            return status;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Conditional decorator node '{NodeName}' condition evaluation failed", Name);
            return BehaviorTreeNodeStatus.Failure;
        }
    }
}
