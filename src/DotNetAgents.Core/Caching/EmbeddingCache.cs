using System.Security.Cryptography;
using System.Text;
using DotNetAgents.Abstractions.Caching;

namespace DotNetAgents.Core.Caching;

/// <summary>
/// Implementation of <see cref="IEmbeddingCache"/> that uses an underlying cache.
/// </summary>
public class EmbeddingCache : IEmbeddingCache
{
    private readonly ICache _cache;
    private readonly TimeSpan _defaultExpiration;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddingCache"/> class.
    /// </summary>
    /// <param name="cache">The underlying cache to use.</param>
    /// <param name="defaultExpiration">The default expiration time for cached embeddings.</param>
    public EmbeddingCache(ICache cache, TimeSpan? defaultExpiration = null)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _defaultExpiration = defaultExpiration ?? TimeSpan.FromDays(7);
    }

    /// <inheritdoc/>
    public async Task<float[]?> GetCachedEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be null or whitespace.", nameof(text));

        var key = GetCacheKey(text);
        return await _cache.GetAsync<float[]>(key, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task CacheEmbeddingAsync(
        string text,
        float[] embedding,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be null or whitespace.", nameof(text));

        if (embedding == null || embedding.Length == 0)
            throw new ArgumentException("Embedding cannot be null or empty.", nameof(embedding));

        var key = GetCacheKey(text);
        await _cache.SetAsync(key, embedding, _defaultExpiration, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        return _cache.ClearAsync(cancellationToken);
    }

    private static string GetCacheKey(string text)
    {
        // Use SHA256 hash of the text as the cache key to ensure consistent keys
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = SHA256.HashData(bytes);
        return $"embedding:{Convert.ToHexString(hash)}";
    }
}
