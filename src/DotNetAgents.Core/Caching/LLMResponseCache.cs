using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DotNetAgents.Abstractions.Caching;

namespace DotNetAgents.Core.Caching;

/// <summary>
/// Implementation of <see cref="ILLMResponseCache{TInput, TOutput}"/> that uses an underlying cache.
/// </summary>
/// <typeparam name="TInput">The type of the input.</typeparam>
/// <typeparam name="TOutput">The type of the output.</typeparam>
public class LLMResponseCache<TInput, TOutput> : ILLMResponseCache<TInput, TOutput> where TOutput : class
{
    private readonly ICache _cache;
    private readonly TimeSpan _defaultExpiration;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="LLMResponseCache{TInput, TOutput}"/> class.
    /// </summary>
    /// <param name="cache">The underlying cache to use.</param>
    /// <param name="defaultExpiration">The default expiration time for cached responses.</param>
    /// <param name="jsonOptions">Optional JSON serializer options.</param>
    public LLMResponseCache(
        ICache cache,
        TimeSpan? defaultExpiration = null,
        JsonSerializerOptions? jsonOptions = null)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _defaultExpiration = defaultExpiration ?? TimeSpan.FromHours(24);
        _jsonOptions = jsonOptions ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc/>
    public async Task<TOutput?> GetCachedResponseAsync(TInput input, CancellationToken cancellationToken = default)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var key = GetCacheKey(input);
        return await _cache.GetAsync<TOutput>(key, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task CacheResponseAsync(
        TInput input,
        TOutput output,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        if (output == null)
            throw new ArgumentNullException(nameof(output));

        var key = GetCacheKey(input);
        await _cache.SetAsync(key, output, expiration ?? _defaultExpiration, cancellationToken).ConfigureAwait(false);
    }

    private string GetCacheKey(TInput input)
    {
        // Serialize input to JSON and hash it to create a consistent cache key
        var json = JsonSerializer.Serialize(input, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var hash = SHA256.HashData(bytes);
        return $"llm_response:{typeof(TInput).Name}:{typeof(TOutput).Name}:{Convert.ToHexString(hash)}";
    }
}
