namespace DotNetAgents.Abstractions.Resilience;

/// <summary>
/// Configuration options for retry policies.
/// </summary>
public record RetryPolicyOptions
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts (default: 3).
    /// </summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// Gets or sets the initial delay between retries (default: 1 second).
    /// </summary>
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets whether to use exponential backoff (default: true).
    /// </summary>
    public bool UseExponentialBackoff { get; init; } = true;

    /// <summary>
    /// Gets or sets the maximum delay between retries (default: 30 seconds).
    /// </summary>
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets a function to determine if an exception should be retried.
    /// </summary>
    public Func<Exception, bool>? ShouldRetry { get; init; }
}
