namespace DotNetAgents.Abstractions.Caching;

/// <summary>
/// Interface for caching LLM responses.
/// </summary>
/// <typeparam name="TInput">The type of the input.</typeparam>
/// <typeparam name="TOutput">The type of the output.</typeparam>
public interface ILLMResponseCache<TInput, TOutput> where TOutput : class
{
    /// <summary>
    /// Gets a cached response for the given input.
    /// </summary>
    /// <param name="input">The input to get the cached response for.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The cached response if found; otherwise, null.</returns>
    Task<TOutput?> GetCachedResponseAsync(TInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Caches a response for the given input.
    /// </summary>
    /// <param name="input">The input associated with the response.</param>
    /// <param name="output">The response to cache.</param>
    /// <param name="expiration">Optional expiration time.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous cache operation.</returns>
    Task CacheResponseAsync(
        TInput input,
        TOutput output,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);
}
