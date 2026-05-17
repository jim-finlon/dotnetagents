namespace DotNetAgents.Abstractions.Models;

/// <summary>
/// Interface for embedding model providers that convert text to vector representations.
/// </summary>
public interface IEmbeddingModel
{
    /// <summary>
    /// Generates an embedding vector for the given text.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A vector representation of the text.</returns>
    Task<float[]> EmbedAsync(
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embedding vectors for multiple texts in batch.
    /// </summary>
    /// <param name="texts">The texts to embed.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>An array of vector representations.</returns>
    Task<float[][]> EmbedBatchAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the dimension of the embedding vectors produced by this model.
    /// </summary>
    int Dimension { get; }

    /// <summary>
    /// Gets the name of the embedding model.
    /// </summary>
    string ModelName { get; }
}
