using System.Collections.Concurrent;
using DotNetAgents.Abstractions.Caching;

namespace DotNetAgents.Core.Caching;

/// <summary>
/// In-memory implementation of <see cref="ICache"/>.
/// </summary>
public class InMemoryCache : ICache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly TimeSpan _defaultExpiration;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryCache"/> class.
    /// </summary>
    /// <param name="defaultExpiration">The default expiration time for cached items.</param>
    public InMemoryCache(TimeSpan? defaultExpiration = null)
    {
        _defaultExpiration = defaultExpiration ?? TimeSpan.FromHours(1);
    }

    /// <inheritdoc/>
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        cancellationToken.ThrowIfCancellationRequested();

        if (!_cache.TryGetValue(key, out var entry))
        {
            return Task.FromResult<T?>(null);
        }

        // Check if expired
        if (entry.ExpiresAt < DateTime.UtcNow)
        {
            _cache.TryRemove(key, out _);
            return Task.FromResult<T?>(null);
        }

        if (entry.Value is T typedValue)
        {
            return Task.FromResult<T?>(typedValue);
        }

        return Task.FromResult<T?>(null);
    }

    /// <inheritdoc/>
    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        if (value == null)
            throw new ArgumentNullException(nameof(value));

        cancellationToken.ThrowIfCancellationRequested();

        var expiresAt = DateTime.UtcNow + (expiration ?? _defaultExpiration);
        var entry = new CacheEntry
        {
            Value = value,
            ExpiresAt = expiresAt
        };

        _cache.AddOrUpdate(key, entry, (_, _) => entry);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(_cache.TryRemove(key, out _));
    }

    /// <inheritdoc/>
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _cache.Clear();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        cancellationToken.ThrowIfCancellationRequested();

        if (!_cache.TryGetValue(key, out var entry))
        {
            return Task.FromResult(false);
        }

        // Check if expired
        if (entry.ExpiresAt < DateTime.UtcNow)
        {
            _cache.TryRemove(key, out _);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    private class CacheEntry
    {
        public object Value { get; init; } = null!;
        public DateTime ExpiresAt { get; init; }
    }
}
