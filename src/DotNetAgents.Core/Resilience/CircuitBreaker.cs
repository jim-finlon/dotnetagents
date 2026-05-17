using DotNetAgents.Abstractions.Resilience;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Core.Resilience;

/// <summary>
/// Provides circuit breaker functionality for async operations.
/// </summary>
public class CircuitBreaker
{
    private readonly CircuitBreakerOptions _options;
    private readonly ILogger<CircuitBreaker>? _logger;
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private int _failureCount;
    private DateTimeOffset _lastFailureTime = DateTimeOffset.MinValue;
    private DateTimeOffset? _openedAt;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreaker"/> class.
    /// </summary>
    /// <param name="options">The circuit breaker options.</param>
    /// <param name="logger">Optional logger for circuit breaker operations.</param>
    public CircuitBreaker(CircuitBreakerOptions? options = null, ILogger<CircuitBreaker>? logger = null)
    {
        _options = options ?? new CircuitBreakerOptions();
        _logger = logger;
    }

    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    public CircuitBreakerState State
    {
        get
        {
            lock (_lock)
            {
                return _state;
            }
        }
    }

    /// <summary>
    /// Executes an async operation with circuit breaker protection.
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

        lock (_lock)
        {
            if (_state == CircuitBreakerState.Open)
            {
                // Check if we should transition to half-open
                if (_openedAt.HasValue && DateTimeOffset.UtcNow - _openedAt.Value >= _options.OpenDuration)
                {
                    _state = CircuitBreakerState.HalfOpen;
                    _logger?.LogInformation("Circuit breaker transitioning to half-open state.");
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Circuit breaker is open. Operations are blocked. Will retry after {_options.OpenDuration.TotalSeconds} seconds.");
                }
            }
        }

        try
        {
            var result = await operation(cancellationToken).ConfigureAwait(false);

            // Success - reset failure count and close circuit if it was half-open
            lock (_lock)
            {
                if (_state == CircuitBreakerState.HalfOpen)
                {
                    _state = CircuitBreakerState.Closed;
                    _failureCount = 0;
                    _openedAt = null;
                    _logger?.LogInformation("Circuit breaker closed after successful operation.");
                }
                else
                {
                    // Reset failure count on success
                    _failureCount = 0;
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            var shouldCountAsFailure = _options.ShouldCountAsFailure?.Invoke(ex) ?? true;

            if (shouldCountAsFailure)
            {
                lock (_lock)
                {
                    _failureCount++;
                    _lastFailureTime = DateTimeOffset.UtcNow;

                    // Check if we should open the circuit
                    if (_state == CircuitBreakerState.HalfOpen)
                    {
                        _state = CircuitBreakerState.Open;
                        _openedAt = DateTimeOffset.UtcNow;
                        _logger?.LogWarning(
                            ex,
                            "Circuit breaker opened after failure in half-open state.");
                    }
                    else if (_failureCount >= _options.FailureThreshold)
                    {
                        // Check if failures are within the failure window
                        var timeSinceFirstFailure = DateTimeOffset.UtcNow - _lastFailureTime;
                        if (timeSinceFirstFailure <= _options.FailureWindow)
                        {
                            _state = CircuitBreakerState.Open;
                            _openedAt = DateTimeOffset.UtcNow;
                            _logger?.LogError(
                                ex,
                                "Circuit breaker opened after {FailureCount} failures within {FailureWindow}s.",
                                _failureCount,
                                _options.FailureWindow.TotalSeconds);
                        }
                        else
                        {
                            // Reset if outside failure window
                            _failureCount = 1;
                        }
                    }
                }
            }

            throw;
        }
    }
}
