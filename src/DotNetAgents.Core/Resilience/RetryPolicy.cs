using DotNetAgents.Abstractions.Resilience;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Core.Resilience;

/// <summary>
/// Provides retry logic for async operations.
/// </summary>
public class RetryPolicy
{
    private readonly RetryPolicyOptions _options;
    private readonly ILogger<RetryPolicy>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryPolicy"/> class.
    /// </summary>
    /// <param name="options">The retry policy options.</param>
    /// <param name="logger">Optional logger for retry operations.</param>
    public RetryPolicy(RetryPolicyOptions? options = null, ILogger<RetryPolicy>? logger = null)
    {
        _options = options ?? new RetryPolicyOptions();
        _logger = logger;
    }

    /// <summary>
    /// Executes an async operation with retry logic.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        Exception? lastException = null;
        var attempt = 0;

        while (attempt <= _options.MaxRetries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await operation(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (attempt < _options.MaxRetries && ShouldRetryException(ex))
            {
                lastException = ex;
                attempt++;

                var delay = CalculateDelay(attempt);
                _logger?.LogWarning(
                    ex,
                    "Operation failed (attempt {Attempt}/{MaxRetries}). Retrying in {Delay}ms.",
                    attempt,
                    _options.MaxRetries,
                    delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        _logger?.LogError(
            lastException,
            "Operation failed after {MaxRetries} retries.",
            _options.MaxRetries);

        throw lastException ?? new InvalidOperationException("Operation failed with no exception captured.");
    }

    /// <summary>
    /// Executes an async operation with retry logic that returns void.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    public async Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await ExecuteAsync<object?>(
            async ct =>
            {
                await operation(ct).ConfigureAwait(false);
                return null;
            },
            cancellationToken).ConfigureAwait(false);
    }

    private bool ShouldRetryException(Exception exception)
    {
        if (_options.ShouldRetry != null)
        {
            return _options.ShouldRetry(exception);
        }

        // Default: retry on transient exceptions
        return exception is HttpRequestException ||
               exception is TaskCanceledException ||
               (exception is AggregateException aggEx && aggEx.InnerExceptions.Any(e => e is HttpRequestException || e is TaskCanceledException));
    }

    private TimeSpan CalculateDelay(int attempt)
    {
        if (!_options.UseExponentialBackoff)
        {
            return _options.InitialDelay;
        }

        // Exponential backoff: delay = initialDelay * 2^(attempt-1)
        var delay = TimeSpan.FromMilliseconds(
            _options.InitialDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));

        return delay > _options.MaxDelay ? _options.MaxDelay : delay;
    }
}
