namespace DotNetAgents.Abstractions.Retrieval;

/// <summary>
/// Interface for vector stores that support similarity search.
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// Upserts (inserts or updates) a vector with the given ID.
    /// </summary>
    /// <param name="id">The unique identifier for the vector.</param>
    /// <param name="vector">The vector to store.</param>
    /// <param name="metadata">Optional metadata associated with the vector.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The ID of the stored vector.</returns>
    Task<string> UpsertAsync(
        string id,
        float[] vector,
        IDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for similar vectors.
    /// </summary>
    /// <param name="queryVector">The query vector to search with.</param>
    /// <param name="topK">The number of results to return.</param>
    /// <param name="filter">Optional metadata filter to apply.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of search results ordered by similarity.</returns>
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] queryVector,
        int topK = 10,
        IDictionary<string, object>? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes vectors by their IDs.
    /// </summary>
    /// <param name="ids">The IDs of the vectors to delete.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The number of vectors deleted.</returns>
    Task<int> DeleteAsync(
        IEnumerable<string> ids,
        CancellationToken cancellationToken = default);
}
