using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// A decorator node that repeats its child a specified number of times.
/// </summary>
/// <typeparam name="TContext">The type of the execution context.</typeparam>
public class RepeaterNode<TContext> : DecoratorNode<TContext> where TContext : class
{
    private readonly int _repeatCount;
    private int _currentCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="RepeaterNode{TContext}"/> class.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="repeatCount">The number of times to repeat. Use -1 for infinite repetition.</param>
    /// <param name="logger">Optional logger instance.</param>
    public RepeaterNode(
        string name,
        int repeatCount,
        ILogger<BehaviorTreeNode<TContext>>? logger = null)
        : base(name, logger)
    {
        if (repeatCount < -1 || repeatCount == 0)
        {
            throw new ArgumentException("Repeat count must be -1 (infinite) or greater than 0.", nameof(repeatCount));
        }

        _repeatCount = repeatCount;
        _currentCount = 0;
    }

    /// <inheritdoc/>
    public override async Task<BehaviorTreeNodeStatus> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (Child == null)
        {
            Logger?.LogWarning("Repeater node '{NodeName}' has no child", Name);
            return BehaviorTreeNodeStatus.Failure;
        }

        Logger?.LogDebug("Executing repeater node '{NodeName}' (iteration {Current}/{Total})",
            Name, _currentCount + 1, _repeatCount == -1 ? "∞" : _repeatCount.ToString());

        while (_repeatCount == -1 || _currentCount < _repeatCount)
        {
            var status = await Child.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);

            if (status == BehaviorTreeNodeStatus.Running)
            {
                Logger?.LogDebug("Repeater node '{NodeName}' is running", Name);
                return BehaviorTreeNodeStatus.Running;
            }

            _currentCount++;

            if (_repeatCount != -1 && _currentCount >= _repeatCount)
            {
                Logger?.LogDebug("Repeater node '{NodeName}' completed {Count} iterations", Name, _currentCount);
                return BehaviorTreeNodeStatus.Success;
            }

            // Reset child for next iteration
            Child.Reset();
        }

        return BehaviorTreeNodeStatus.Success;
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        base.Reset();
        _currentCount = 0;
    }
}
