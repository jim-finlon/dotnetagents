// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.AgenticRAG;

/// <summary>
/// A single RAG retrieval strategy (e.g. direct, HyDE, multi-query). Used by adaptive RAG.
/// FR-RAG-003.
/// </summary>
public interface IRetrievalStrategy
{
    /// <summary>Strategy name (e.g. "direct", "hyde", "multi_query").</summary>
    string Name { get; }

    /// <summary>Retrieve documents for the query using this strategy.</summary>
    Task<IReadOnlyList<RetrievedDocument>> RetrieveAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default);
}
