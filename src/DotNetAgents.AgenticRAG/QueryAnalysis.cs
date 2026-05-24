// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.AgenticRAG;

/// <summary>Result of query complexity/composition analysis for adaptive strategy selection.</summary>
public sealed record QueryAnalysis
{
    /// <summary>Estimated complexity: Simple, Moderate, Complex.</summary>
    public QueryComplexity Complexity { get; init; }

    /// <summary>Suggested strategy name (e.g. "direct", "hyde", "multi_query").</summary>
    public string? SuggestedStrategy { get; init; }

    /// <summary>Whether the query is multi-faceted (benefits from multi-query or expansion).</summary>
    public bool IsMultiFaceted { get; init; }
}

/// <summary>Query complexity level.</summary>
public enum QueryComplexity
{
    Simple,
    Moderate,
    Complex
}
