// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.Models;
using DotNetAgents.Abstractions.Retrieval;

namespace DotNetAgents.AgenticRAG;

/// <summary>
/// Self-RAG implementation: uses a decision function (e.g. LLM-based) and vector store + embeddings for retrieval.
/// FR-RAG-001.
/// </summary>
public sealed class SelfRAG : ISelfRAG
{
    private readonly Func<string, string?, CancellationToken, Task<RetrievalDecision>> _shouldRetrieve;
    private readonly IVectorStore _store;
    private readonly IEmbeddingModel _embeddings;
    private readonly int _defaultTopK;

    /// <summary>
    /// Builds Self-RAG with a custom decision delegate and retrieval via vector store.
    /// </summary>
    /// <param name="shouldRetrieve">Async delegate (query, context, ct) => decision. Use an LLM or heuristic.</param>
    /// <param name="store">Vector store for retrieval.</param>
    /// <param name="embeddings">Embedding model for query.</param>
    /// <param name="defaultTopK">Default number of documents to retrieve.</param>
    public SelfRAG(
        Func<string, string?, CancellationToken, Task<RetrievalDecision>> shouldRetrieve,
        IVectorStore store,
        IEmbeddingModel embeddings,
        int defaultTopK = 5)
    {
        _shouldRetrieve = shouldRetrieve ?? throw new ArgumentNullException(nameof(shouldRetrieve));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _embeddings = embeddings ?? throw new ArgumentNullException(nameof(embeddings));
        _defaultTopK = defaultTopK > 0 ? defaultTopK : 5;
    }

    /// <inheritdoc />
    public async Task<RetrievalDecision> ShouldRetrieveAsync(string query, string? context = null, bool forceRetrieve = false, CancellationToken cancellationToken = default)
    {
        if (forceRetrieve) return RetrievalDecision.ForceRetrieve;
        return await _shouldRetrieve(query, context, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RetrievedDocument>> RetrieveIfNeededAsync(
        string query,
        string? context = null,
        bool forceRetrieve = false,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        var decision = await ShouldRetrieveAsync(query, context, forceRetrieve, cancellationToken).ConfigureAwait(false);
        if (decision == RetrievalDecision.Skip)
            return Array.Empty<RetrievedDocument>();
        var k = topK > 0 ? topK : _defaultTopK;
        var vector = await _embeddings.EmbedAsync(query, cancellationToken).ConfigureAwait(false);
        var results = await _store.SearchAsync(vector, k, null, cancellationToken).ConfigureAwait(false);
        return results.Select(r => new RetrievedDocument(
            r.Id,
            r.Metadata?.TryGetValue("content", out var c) == true && c is string s ? s : string.Empty,
            r.Score,
            r.Metadata)).ToList();
    }
}
