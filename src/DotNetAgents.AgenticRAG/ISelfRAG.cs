namespace DotNetAgents.AgenticRAG;

/// <summary>
/// Self-RAG: decides whether to retrieve for a query given context, then optionally retrieves.
/// FR-RAG-001.
/// </summary>
public interface ISelfRAG
{
    /// <summary>Decides whether to retrieve documents for the query given current context.</summary>
    /// <param name="query">User or agent query.</param>
    /// <param name="context">Optional conversation or context summary.</param>
    /// <param name="forceRetrieve">When true, return ForceRetrieve.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    Task<RetrievalDecision> ShouldRetrieveAsync(
        string query,
        string? context = null,
        bool forceRetrieve = false,
        CancellationToken cancellationToken = default);

    /// <summary>If ShouldRetrieveAsync says Retrieve/ForceRetrieve, retrieves documents; otherwise returns empty.</summary>
    Task<IReadOnlyList<RetrievedDocument>> RetrieveIfNeededAsync(
        string query,
        string? context = null,
        bool forceRetrieve = false,
        int topK = 5,
        CancellationToken cancellationToken = default);
}

/// <summary>A document returned by RAG retrieval.</summary>
public sealed record RetrievedDocument(string Id, string Content, float Score, IDictionary<string, object>? Metadata = null);
