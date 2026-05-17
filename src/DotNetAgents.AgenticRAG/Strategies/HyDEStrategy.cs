using DotNetAgents.Abstractions.Models;
using DotNetAgents.Abstractions.Retrieval;

namespace DotNetAgents.AgenticRAG.Strategies;

/// <summary>
/// HyDE (Hypothetical Document Embeddings): generate a hypothetical answer, embed it, search with that embedding.
/// FR-RAG-004.
/// </summary>
public sealed class HyDEStrategy : IRetrievalStrategy
{
    private readonly IVectorStore _store;
    private readonly IEmbeddingModel _embeddings;
    private readonly ILLMModel<string, string> _llm;
    private readonly string _promptTemplate;

    public HyDEStrategy(
        IVectorStore store,
        IEmbeddingModel embeddings,
        ILLMModel<string, string> llm,
        string? promptTemplate = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _embeddings = embeddings ?? throw new ArgumentNullException(nameof(embeddings));
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _promptTemplate = promptTemplate ?? "Write a short hypothetical answer to this question (2-3 sentences): {0}";
    }

    /// <inheritdoc />
    public string Name => "hyde";

    /// <inheritdoc />
    public async Task<IReadOnlyList<RetrievedDocument>> RetrieveAsync(string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        var prompt = string.Format(_promptTemplate, query);
        var hypotheticalAnswer = await _llm.GenerateAsync(prompt, null, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(hypotheticalAnswer))
            hypotheticalAnswer = query;
        var vector = await _embeddings.EmbedAsync(hypotheticalAnswer, cancellationToken).ConfigureAwait(false);
        var results = await _store.SearchAsync(vector, topK, null, cancellationToken).ConfigureAwait(false);
        return results.Select(r => new RetrievedDocument(
            r.Id,
            r.Metadata?.TryGetValue("content", out var c) == true && c is string s ? s : string.Empty,
            r.Score,
            r.Metadata)).ToList();
    }
}
