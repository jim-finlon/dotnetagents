namespace DotNetAgents.Abstractions.Resilience;

/// <summary>
/// States of a circuit breaker.
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>
    /// Circuit is closed - operations are allowed.
    /// </summary>
    Closed,

    /// <summary>
    /// Circuit is open - operations are blocked.
    /// </summary>
    Open,

    /// <summary>
    /// Circuit is half-open - testing if service has recovered.
    /// </summary>
    HalfOpen
}

/// <summary>
/// Configuration options for circuit breaker.
/// </summary>
public record CircuitBreakerOptions
{
    /// <summary>
    /// Gets or sets the failure threshold before opening the circuit (default: 5).
    /// </summary>
    public int FailureThreshold { get; init; } = 5;

    /// <summary>
    /// Gets or sets the duration the circuit stays open before transitioning to half-open (default: 30 seconds).
    /// </summary>
    public TimeSpan OpenDuration { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the time window for counting failures (default: 60 seconds).
    /// </summary>
    public TimeSpan FailureWindow { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets a function to determine if an exception should count as a failure.
    /// </summary>
    public Func<Exception, bool>? ShouldCountAsFailure { get; init; }
}
