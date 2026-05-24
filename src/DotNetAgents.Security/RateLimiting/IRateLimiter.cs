// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Security.RateLimiting;

/// <summary>
/// Interface for rate limiting operations.
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Attempts to acquire a permit for the specified key.
    /// </summary>
    /// <param name="key">The key to rate limit.</param>
    /// <param name="limit">The maximum number of requests allowed.</param>
    /// <param name="window">The time window for the limit.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>True if the permit was acquired; otherwise, false.</returns>
    Task<bool> TryAcquireAsync(
        string key,
        int limit,
        TimeSpan window,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the number of remaining permits for the specified key.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <param name="limit">The maximum number of requests allowed.</param>
    /// <param name="window">The time window for the limit.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The number of remaining permits.</returns>
    Task<int> GetRemainingAsync(
        string key,
        int limit,
        TimeSpan window,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the rate limit for the specified key.
    /// </summary>
    /// <param name="key">The key to reset.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous reset operation.</returns>
    Task ResetAsync(string key, CancellationToken cancellationToken = default);
}
