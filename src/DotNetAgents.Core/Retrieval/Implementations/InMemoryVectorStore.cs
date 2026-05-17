using DotNetAgents.Abstractions.Retrieval;
using DotNetAgents.Core.Retrieval;

namespace DotNetAgents.Core.Retrieval.Implementations;

/// <summary>
/// In-memory implementation of <see cref="IVectorStore"/> for testing and development.
/// </summary>
public class InMemoryVectorStore : IVectorStore
{
    private readonly Dictionary<string, (float[] Vector, IDictionary<string, object>? Metadata)> _vectors = new();
    private readonly object _lock = new();

    /// <inheritdoc/>
    public Task<string> UpsertAsync(
        string id,
        float[] vector,
        IDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID cannot be null or whitespace.", nameof(id));
        if (vector == null || vector.Length == 0)
            throw new ArgumentException("Vector cannot be null or empty.", nameof(vector));

        lock (_lock)
        {
            _vectors[id] = (vector, metadata);
        }

        return Task.FromResult(id);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] queryVector,
        int topK = 10,
        IDictionary<string, object>? filter = null,
        CancellationToken cancellationToken = default)
    {
        if (queryVector == null || queryVector.Length == 0)
            throw new ArgumentException("Query vector cannot be null or empty.", nameof(queryVector));
        if (topK <= 0)
            throw new ArgumentException("TopK must be positive.", nameof(topK));

        lock (_lock)
        {
            var results = new List<VectorSearchResult>();

            foreach (var (id, (vector, metadata)) in _vectors)
            {
                // Apply metadata filter if provided
                if (filter != null && metadata != null)
                {
                    var matchesFilter = filter.All(kvp =>
                        metadata.TryGetValue(kvp.Key, out var value) &&
                        Equals(value, kvp.Value));

                    if (!matchesFilter)
                    {
                        continue;
                    }
                }

                // Calculate cosine similarity using optimized SIMD operations
                var similarity = VectorOperations.CosineSimilarity(queryVector, vector);

                results.Add(new VectorSearchResult
                {
                    Id = id,
                    Score = similarity,
                    Metadata = metadata
                });
            }

            // Sort by similarity (descending) and take top K
            return Task.FromResult<IReadOnlyList<VectorSearchResult>>(
                results
                    .OrderByDescending(r => r.Score)
                    .Take(topK)
                    .ToList()
                    .AsReadOnly());
        }
    }

    /// <inheritdoc/>
    public Task<int> DeleteAsync(
        IEnumerable<string> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids == null)
            throw new ArgumentNullException(nameof(ids));

        lock (_lock)
        {
            var count = 0;
            foreach (var id in ids)
            {
                if (_vectors.Remove(id))
                {
                    count++;
                }
            }

            return Task.FromResult(count);
        }
    }

}
