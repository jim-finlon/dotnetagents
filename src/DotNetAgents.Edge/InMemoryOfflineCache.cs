using Microsoft.Extensions.Logging;

namespace DotNetAgents.Edge;

/// <summary>
/// In-memory implementation of offline cache.
/// </summary>
public class InMemoryOfflineCache : IOfflineCache
{
    private readonly Dictionary<string, CacheEntry> _cache = new();
    private readonly ILogger<InMemoryOfflineCache>? _logger;
    private readonly object _lock = new();
    private long _totalSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryOfflineCache"/> class.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    public InMemoryOfflineCache(ILogger<InMemoryOfflineCache>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                // Check expiration
                if (entry.ExpiresAt.HasValue && DateTimeOffset.UtcNow > entry.ExpiresAt.Value)
                {
                    _cache.Remove(key);
                    _totalSize -= entry.Size;
                    return Task.FromResult<string?>(null);
                }

                return Task.FromResult<string?>(entry.Value);
            }
        }

        return Task.FromResult<string?>(null);
    }

    /// <inheritdoc />
    public Task SetAsync(
        string key,
        string value,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);
        cancellationToken.ThrowIfCancellationRequested();

        var size = System.Text.Encoding.UTF8.GetByteCount(value);
        var expiresAt = ttl.HasValue ? DateTimeOffset.UtcNow.Add(ttl.Value) : (DateTimeOffset?)null;

        lock (_lock)
        {
            // Remove old entry if exists
            if (_cache.TryGetValue(key, out var oldEntry))
            {
                _totalSize -= oldEntry.Size;
            }

            _cache[key] = new CacheEntry
            {
                Value = value,
                ExpiresAt = expiresAt,
                Size = size
            };

            _totalSize += size;
        }

        _logger?.LogDebug("Cached value for key {Key} (size: {Size} bytes)", key, size);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                _cache.Remove(key);
                _totalSize -= entry.Size;
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _cache.Clear();
            _totalSize = 0;
        }

        _logger?.LogInformation("Cleared offline cache");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<long> GetSizeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            return Task.FromResult(_totalSize);
        }
    }

    private class CacheEntry
    {
        public string Value { get; set; } = string.Empty;
        public DateTimeOffset? ExpiresAt { get; set; }
        public long Size { get; set; }
    }
}
