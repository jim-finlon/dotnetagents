namespace DotNetAgents.Abstractions.Retrieval;

/// <summary>
/// Represents a result from vector similarity search.
/// </summary>
public record VectorSearchResult
{
    /// <summary>
    /// Gets or sets the ID of the vector.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the similarity score (higher is more similar).
    /// </summary>
    public float Score { get; init; }

    /// <summary>
    /// Gets or sets the metadata associated with the vector.
    /// </summary>
    public IDictionary<string, object>? Metadata { get; init; }
}
