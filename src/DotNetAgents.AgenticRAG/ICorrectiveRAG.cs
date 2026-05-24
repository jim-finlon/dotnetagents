// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.AgenticRAG;

/// <summary>
/// Corrective RAG: scores initial results, re-queries if relevance below threshold (with optional query reformulation).
/// FR-RAG-002.
/// </summary>
public interface ICorrectiveRAG
{
    /// <summary>Retrieves and optionally re-queries if relevance is below threshold.</summary>
    /// <param name="query">Original query.</param>
    /// <param name="initialResults">Initial retrieval results (can be empty to perform first retrieval).</param>
    /// <param name="relevanceThreshold">Score threshold below which to re-query or reformulate (e.g. 0.5).</param>
    /// <param name="maxRounds">Maximum correction rounds.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    Task<IReadOnlyList<RetrievedDocument>> RetrieveWithCorrectionAsync(
        string query,
        IReadOnlyList<RetrievedDocument>? initialResults = null,
        float relevanceThreshold = 0.5f,
        int maxRounds = 2,
        CancellationToken cancellationToken = default);
}
