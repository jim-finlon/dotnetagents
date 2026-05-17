namespace DotNetAgents.Abstractions.Caching;

/// <summary>
/// Interface for caching embeddings.
/// </summary>
public interface IEmbeddingCache
{
    /// <summary>
    /// Gets a cached embedding for the given text.
    /// </summary>
    /// <param name="text">The text to get the embedding for.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The cached embedding if found; otherwise, null.</returns>
    Task<float[]?> GetCachedEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Caches an embedding for the given text.
    /// </summary>
    /// <param name="text">The text associated with the embedding.</param>
    /// <param name="embedding">The embedding vector to cache.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous cache operation.</returns>
    Task CacheEmbeddingAsync(
        string text,
        float[] embedding,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cached embeddings.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous clear operation.</returns>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
