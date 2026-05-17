using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// A decorator node that rate-limits child execution with a cooldown period.
/// Returns Running if still in cooldown, otherwise executes the child.
/// </summary>
/// <typeparam name="TContext">The type of the execution context.</typeparam>
public class CooldownNode<TContext> : DecoratorNode<TContext> where TContext : class
{
    private readonly TimeSpan _cooldownDuration;
    private DateTimeOffset _lastExecutionTime = DateTimeOffset.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="CooldownNode{TContext}"/> class.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="cooldownDuration">The cooldown duration between executions.</param>
    /// <param name="logger">Optional logger instance.</param>
    public CooldownNode(
        string name,
        TimeSpan cooldownDuration,
        ILogger<BehaviorTreeNode<TContext>>? logger = null)
        : base(name, logger)
    {
        if (cooldownDuration <= TimeSpan.Zero)
        {
            throw new ArgumentException("Cooldown duration must be greater than zero.", nameof(cooldownDuration));
        }

        _cooldownDuration = cooldownDuration;
    }

    /// <inheritdoc/>
    public override async Task<BehaviorTreeNodeStatus> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (Child == null)
        {
            Logger?.LogWarning("Cooldown node '{NodeName}' has no child", Name);
            return BehaviorTreeNodeStatus.Failure;
        }

        var now = DateTimeOffset.UtcNow;
        var timeSinceLastExecution = now - _lastExecutionTime;

        if (timeSinceLastExecution < _cooldownDuration)
        {
            var remainingCooldown = _cooldownDuration - timeSinceLastExecution;
            Logger?.LogDebug("Cooldown node '{NodeName}' is in cooldown for {Remaining}ms", Name, remainingCooldown.TotalMilliseconds);
            return BehaviorTreeNodeStatus.Running;
        }

        Logger?.LogDebug("Executing cooldown node '{NodeName}' (cooldown expired)", Name);

        _lastExecutionTime = now;
        var status = await Child.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);

        Logger?.LogDebug("Cooldown node '{NodeName}' completed with status '{Status}'", Name, status);
        return status;
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        base.Reset();
        _lastExecutionTime = DateTimeOffset.MinValue;
    }
}
