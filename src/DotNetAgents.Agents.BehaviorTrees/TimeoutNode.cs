using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// A decorator node that adds a timeout to child execution.
/// Returns Failure if the timeout is exceeded, otherwise returns the child's status.
/// </summary>
/// <typeparam name="TContext">The type of the execution context.</typeparam>
public class TimeoutNode<TContext> : DecoratorNode<TContext> where TContext : class
{
    private readonly TimeSpan _timeout;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeoutNode{TContext}"/> class.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <param name="logger">Optional logger instance.</param>
    public TimeoutNode(
        string name,
        TimeSpan timeout,
        ILogger<BehaviorTreeNode<TContext>>? logger = null)
        : base(name, logger)
    {
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentException("Timeout must be greater than zero.", nameof(timeout));
        }

        _timeout = timeout;
    }

    /// <inheritdoc/>
    public override async Task<BehaviorTreeNodeStatus> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (Child == null)
        {
            Logger?.LogWarning("Timeout node '{NodeName}' has no child", Name);
            return BehaviorTreeNodeStatus.Failure;
        }

        Logger?.LogDebug("Executing timeout node '{NodeName}' with timeout {Timeout}", Name, _timeout);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_timeout);

        try
        {
            var status = await Child.ExecuteAsync(context, timeoutCts.Token).ConfigureAwait(false);
            Logger?.LogDebug("Timeout node '{NodeName}' completed with status '{Status}'", Name, status);
            return status;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            Logger?.LogWarning("Timeout node '{NodeName}' timed out after {Timeout}", Name, _timeout);
            return BehaviorTreeNodeStatus.Failure;
        }
    }
}
