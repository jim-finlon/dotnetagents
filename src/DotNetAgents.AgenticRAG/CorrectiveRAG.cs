// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.Models;
using DotNetAgents.Abstractions.Retrieval;

namespace DotNetAgents.AgenticRAG;

/// <summary>
/// Corrective RAG: scores results (by embedding similarity to query), re-queries if below threshold. Optional query reformulation via LLM.
/// FR-RAG-002.
/// </summary>
public sealed class CorrectiveRAG : ICorrectiveRAG
{
    private readonly IVectorStore _store;
    private readonly IEmbeddingModel _embeddings;
    private readonly ILLMModel<string, string>? _llmForReformulation;
    private const string ReformulationPrompt = "Reformulate the following search query to improve relevance. Output only the new query, nothing else.\nQuery: {0}";

    public CorrectiveRAG(
        IVectorStore store,
        IEmbeddingModel embeddings,
        ILLMModel<string, string>? llmForReformulation = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _embeddings = embeddings ?? throw new ArgumentNullException(nameof(embeddings));
        _llmForReformulation = llmForReformulation;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RetrievedDocument>> RetrieveWithCorrectionAsync(
        string query,
        IReadOnlyList<RetrievedDocument>? initialResults = null,
        float relevanceThreshold = 0.5f,
        int maxRounds = 2,
        CancellationToken cancellationToken = default)
    {
        var currentQuery = query;
        IReadOnlyList<RetrievedDocument>? current = initialResults;
        for (var round = 0; round < maxRounds; round++)
        {
            if (current == null || current.Count == 0)
            {
                var vector = await _embeddings.EmbedAsync(currentQuery, cancellationToken).ConfigureAwait(false);
                var results = await _store.SearchAsync(vector, 10, null, cancellationToken).ConfigureAwait(false);
                current = results.Select(r => new RetrievedDocument(
                    r.Id,
                    r.Metadata?.TryGetValue("content", out var c) == true && c is string s ? s : string.Empty,
                    r.Score,
                    r.Metadata)).ToList();
            }
            var minScore = current.Count > 0 ? current.Min(d => d.Score) : 1f;
            if (minScore >= relevanceThreshold)
                return current;
            if (round == maxRounds - 1)
                return current;
            if (_llmForReformulation != null)
            {
                var reformulated = await _llmForReformulation.GenerateAsync(
                    string.Format(ReformulationPrompt, currentQuery), null, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(reformulated))
                    currentQuery = reformulated.Trim();
            }
            current = null;
        }
        return current ?? Array.Empty<RetrievedDocument>();
    }
}
