using DotNetAgents.Abstractions.Exceptions;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Workflow.Graph;

/// <summary>
/// A workflow node that retries execution of a child node on failure with configurable retry logic.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public class RetryNode<TState> : GraphNode<TState> where TState : class
{
    private readonly GraphNode<TState> _childNode;
    private readonly int _maxRetries;
    private readonly TimeSpan _initialDelay;
    private readonly double _backoffMultiplier;
    private readonly Func<Exception, bool>? _retryCondition;
    private readonly ILogger<RetryNode<TState>>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryNode{TState}"/> class.
    /// </summary>
    /// <param name="name">The name of the retry node.</param>
    /// <param name="childNode">The child node to retry on failure.</param>
    /// <param name="maxRetries">Maximum number of retry attempts. Default is 3.</param>
    /// <param name="initialDelay">Initial delay before first retry. Default is 1 second.</param>
    /// <param name="backoffMultiplier">Multiplier for exponential backoff. Default is 2.0.</param>
    /// <param name="retryCondition">Optional function that determines if an exception should be retried. If null, all exceptions are retried.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    /// <exception cref="ArgumentException">Thrown when maxRetries is less than 1.</exception>
    public RetryNode(
        string name,
        GraphNode<TState> childNode,
        int maxRetries = 3,
        TimeSpan? initialDelay = null,
        double backoffMultiplier = 2.0,
        Func<Exception, bool>? retryCondition = null,
        ILogger<RetryNode<TState>>? logger = null)
        : base(name, CreateHandler(
            childNode ?? throw new ArgumentNullException(nameof(childNode)),
            maxRetries < 1 ? throw new ArgumentException("MaxRetries must be at least 1.", nameof(maxRetries)) : maxRetries,
            initialDelay ?? TimeSpan.FromSeconds(1),
            backoffMultiplier <= 0 ? throw new ArgumentException("BackoffMultiplier must be greater than 0.", nameof(backoffMultiplier)) : backoffMultiplier,
            retryCondition,
            logger,
            name))
    {
        _childNode = childNode;
        _maxRetries = maxRetries;
        _initialDelay = initialDelay ?? TimeSpan.FromSeconds(1);
        _backoffMultiplier = backoffMultiplier;
        _retryCondition = retryCondition;
        _logger = logger;
        Description = $"Retries {childNode.Name} up to {maxRetries} times with exponential backoff";
    }

    private static Func<TState, CancellationToken, Task<TState>> CreateHandler(
        GraphNode<TState> childNode,
        int maxRetries,
        TimeSpan initialDelay,
        double backoffMultiplier,
        Func<Exception, bool>? retryCondition,
        ILogger<RetryNode<TState>>? logger,
        string nodeName)
    {
        return async (state, ct) =>
        {
            ArgumentNullException.ThrowIfNull(state);
            ct.ThrowIfCancellationRequested();

            var attempt = 0;
            Exception? lastException = null;
            var currentDelay = initialDelay;

            logger?.LogInformation(
                "Node {NodeName}: Starting retry execution. Max retries: {MaxRetries}, Initial delay: {InitialDelay}",
                nodeName,
                maxRetries,
                initialDelay);

            while (attempt <= maxRetries)
            {
                attempt++;
                ct.ThrowIfCancellationRequested();

                try
                {
                    logger?.LogDebug(
                        "Node {NodeName}: Attempt {Attempt}/{MaxAttempts}",
                        nodeName,
                        attempt,
                        maxRetries + 1);

                    var result = await childNode.ExecuteAsync(state, ct).ConfigureAwait(false);

                    if (attempt > 1)
                    {
                        logger?.LogInformation(
                            "Node {NodeName}: Successfully executed after {Attempts} attempts.",
                            nodeName,
                            attempt);
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    // Check if we should retry this exception
                    if (retryCondition != null && !retryCondition(ex))
                    {
                        logger?.LogWarning(
                            "Node {NodeName}: Exception '{ExceptionType}' is not retryable. Aborting retries.",
                            nodeName,
                            ex.GetType().Name);
                        throw;
                    }

                    // Check if we've exhausted retries
                    if (attempt > maxRetries)
                    {
                        logger?.LogError(
                            ex,
                            "Node {NodeName}: All {MaxRetries} retry attempts exhausted.",
                            nodeName,
                            maxRetries);
                        throw new AgentException(
                            $"Retry node '{nodeName}' failed after {maxRetries} retry attempts. Last error: {ex.Message}",
                            ErrorCategory.WorkflowError,
                            ex);
                    }

                    logger?.LogWarning(
                        ex,
                        "Node {NodeName}: Attempt {Attempt} failed. Retrying in {Delay}...",
                        nodeName,
                        attempt,
                        currentDelay);

                    // Wait before retrying (exponential backoff)
                    await Task.Delay(currentDelay, ct).ConfigureAwait(false);
                    currentDelay = TimeSpan.FromMilliseconds(currentDelay.TotalMilliseconds * backoffMultiplier);
                }
            }

            // This should never be reached, but compiler requires it
            throw new AgentException(
                $"Retry node '{nodeName}' failed unexpectedly.",
                ErrorCategory.WorkflowError,
                lastException);
        };
    }
}
