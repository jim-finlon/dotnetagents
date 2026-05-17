using System.Collections.Concurrent;

namespace DotNetAgents.Security.RateLimiting;

/// <summary>
/// In-memory implementation of <see cref="IRateLimiter"/> using a sliding window algorithm.
/// </summary>
public class InMemoryRateLimiter : IRateLimiter
{
    private readonly ConcurrentDictionary<string, List<DateTime>> _requests = new();

    /// <inheritdoc/>
    public Task<bool> TryAcquireAsync(
        string key,
        int limit,
        TimeSpan window,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        if (limit <= 0)
            throw new ArgumentException("Limit must be greater than zero.", nameof(limit));

        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTime.UtcNow;
        var cutoff = now - window;

        var requests = _requests.GetOrAdd(key, _ => new List<DateTime>());

        lock (requests)
        {
            // Remove old requests outside the window
            requests.RemoveAll(timestamp => timestamp < cutoff);

            // Check if we're at the limit
            if (requests.Count >= limit)
            {
                return Task.FromResult(false);
            }

            // Add the current request
            requests.Add(now);
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc/>
    public Task<int> GetRemainingAsync(
        string key,
        int limit,
        TimeSpan window,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        if (limit <= 0)
            throw new ArgumentException("Limit must be greater than zero.", nameof(limit));

        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTime.UtcNow;
        var cutoff = now - window;

        if (!_requests.TryGetValue(key, out var requests))
        {
            return Task.FromResult(limit);
        }

        lock (requests)
        {
            // Remove old requests outside the window
            requests.RemoveAll(timestamp => timestamp < cutoff);

            var remaining = Math.Max(0, limit - requests.Count);
            return Task.FromResult(remaining);
        }
    }

    /// <inheritdoc/>
    public Task ResetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        cancellationToken.ThrowIfCancellationRequested();

        _requests.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
