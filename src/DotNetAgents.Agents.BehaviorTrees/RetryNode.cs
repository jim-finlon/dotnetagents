using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// A decorator node that retries the child on failure with exponential backoff.
/// </summary>
/// <typeparam name="TContext">The type of the execution context.</typeparam>
public class RetryNode<TContext> : DecoratorNode<TContext> where TContext : class
{
    private readonly int _maxRetries;
    private readonly TimeSpan _initialDelay;
    private readonly double _backoffMultiplier;
    private readonly TimeSpan? _maxDelay;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryNode{TContext}"/> class.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="maxRetries">The maximum number of retries (including initial attempt).</param>
    /// <param name="initialDelay">The initial delay before the first retry.</param>
    /// <param name="backoffMultiplier">The multiplier for exponential backoff (default: 2.0).</param>
    /// <param name="maxDelay">The maximum delay between retries (optional).</param>
    /// <param name="logger">Optional logger instance.</param>
    public RetryNode(
        string name,
        int maxRetries,
        TimeSpan initialDelay,
        double backoffMultiplier = 2.0,
        TimeSpan? maxDelay = null,
        ILogger<BehaviorTreeNode<TContext>>? logger = null)
        : base(name, logger)
    {
        if (maxRetries < 1)
        {
            throw new ArgumentException("Max retries must be at least 1.", nameof(maxRetries));
        }

        if (initialDelay < TimeSpan.Zero)
        {
            throw new ArgumentException("Initial delay must be non-negative.", nameof(initialDelay));
        }

        if (backoffMultiplier <= 0)
        {
            throw new ArgumentException("Backoff multiplier must be greater than zero.", nameof(backoffMultiplier));
        }

        _maxRetries = maxRetries;
        _initialDelay = initialDelay;
        _backoffMultiplier = backoffMultiplier;
        _maxDelay = maxDelay;
    }

    /// <inheritdoc/>
    public override async Task<BehaviorTreeNodeStatus> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (Child == null)
        {
            Logger?.LogWarning("Retry node '{NodeName}' has no child", Name);
            return BehaviorTreeNodeStatus.Failure;
        }

        Logger?.LogDebug("Executing retry node '{NodeName}' with max {MaxRetries} retries", Name, _maxRetries);

        for (int attempt = 0; attempt < _maxRetries; attempt++)
        {
            if (attempt > 0)
            {
                var delay = CalculateDelay(attempt - 1);
                Logger?.LogDebug("Retry node '{NodeName}' waiting {Delay}ms before retry {Attempt}/{MaxRetries}",
                    Name, delay.TotalMilliseconds, attempt + 1, _maxRetries);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }

            var status = await Child.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);

            if (status == BehaviorTreeNodeStatus.Success)
            {
                Logger?.LogDebug("Retry node '{NodeName}' succeeded on attempt {Attempt}", Name, attempt + 1);
                return BehaviorTreeNodeStatus.Success;
            }

            if (status == BehaviorTreeNodeStatus.Running)
            {
                Logger?.LogDebug("Retry node '{NodeName}' is running", Name);
                return BehaviorTreeNodeStatus.Running;
            }

            // Failure - will retry if attempts remain
            Logger?.LogDebug("Retry node '{NodeName}' failed on attempt {Attempt}/{MaxRetries}", Name, attempt + 1, _maxRetries);
            Child.Reset();
        }

        Logger?.LogWarning("Retry node '{NodeName}' exhausted all {MaxRetries} retries", Name, _maxRetries);
        return BehaviorTreeNodeStatus.Failure;
    }

    private TimeSpan CalculateDelay(int attemptNumber)
    {
        var delay = TimeSpan.FromMilliseconds(_initialDelay.TotalMilliseconds * Math.Pow(_backoffMultiplier, attemptNumber));

        if (_maxDelay.HasValue && delay > _maxDelay.Value)
        {
            return _maxDelay.Value;
        }

        return delay;
    }
}
