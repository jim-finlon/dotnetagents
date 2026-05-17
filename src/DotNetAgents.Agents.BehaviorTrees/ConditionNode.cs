using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// A leaf node that checks a condition.
/// Returns Success if the condition is true, Failure otherwise.
/// </summary>
/// <typeparam name="TContext">The type of the execution context.</typeparam>
public class ConditionNode<TContext> : BehaviorTreeNode<TContext> where TContext : class
{
    private readonly Func<TContext, bool> _condition;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConditionNode{TContext}"/> class.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="condition">The condition to check.</param>
    /// <param name="logger">Optional logger instance.</param>
    public ConditionNode(
        string name,
        Func<TContext, bool> condition,
        ILogger<BehaviorTreeNode<TContext>>? logger = null)
        : base(name, logger)
    {
        _condition = condition ?? throw new ArgumentNullException(nameof(condition));
    }

    /// <inheritdoc/>
    public override Task<BehaviorTreeNodeStatus> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        Logger?.LogDebug("Evaluating condition node '{NodeName}'", Name);

        try
        {
            var result = _condition(context);
            var status = result ? BehaviorTreeNodeStatus.Success : BehaviorTreeNodeStatus.Failure;
            Logger?.LogDebug("Condition node '{NodeName}' evaluated to '{Status}'", Name, status);
            return Task.FromResult(status);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Condition node '{NodeName}' failed", Name);
            return Task.FromResult(BehaviorTreeNodeStatus.Failure);
        }
    }
}
