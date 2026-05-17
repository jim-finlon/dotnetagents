using DotNetAgents.Abstractions.Models;
using DotNetAgents.Abstractions.Retrieval;

namespace DotNetAgents.AgenticRAG.Strategies;

/// <summary>Direct retrieval: embed query, search vector store. FR-RAG-003.</summary>
public sealed class DirectRetrievalStrategy : IRetrievalStrategy
{
    private readonly IVectorStore _store;
    private readonly IEmbeddingModel _embeddings;

    public DirectRetrievalStrategy(IVectorStore store, IEmbeddingModel embeddings)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _embeddings = embeddings ?? throw new ArgumentNullException(nameof(embeddings));
    }

    /// <inheritdoc />
    public string Name => "direct";

    /// <inheritdoc />
    public async Task<IReadOnlyList<RetrievedDocument>> RetrieveAsync(string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        var vector = await _embeddings.EmbedAsync(query, cancellationToken).ConfigureAwait(false);
        var results = await _store.SearchAsync(vector, topK, null, cancellationToken).ConfigureAwait(false);
        return results.Select(r => ToDocument(r)).ToList();
    }

    private static RetrievedDocument ToDocument(VectorSearchResult r)
    {
        var content = r.Metadata?.TryGetValue("content", out var c) == true && c is string s ? s : string.Empty;
        return new RetrievedDocument(r.Id, content, r.Score, r.Metadata);
    }
}
