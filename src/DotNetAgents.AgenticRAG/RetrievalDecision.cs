// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.AgenticRAG;

/// <summary>Decision from Self-RAG: whether to retrieve or skip for the given query/context.</summary>
public enum RetrievalDecision
{
    /// <summary>Retrieve documents for this query.</summary>
    Retrieve,

    /// <summary>Skip retrieval (e.g. query is answerable from context or general knowledge).</summary>
    Skip,

    /// <summary>Force retrieval regardless of confidence (e.g. user override).</summary>
    ForceRetrieve
}
