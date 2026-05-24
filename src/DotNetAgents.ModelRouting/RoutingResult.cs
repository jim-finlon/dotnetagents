// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.ModelRouting;

/// <summary>Result of a routing decision: selected model or endpoint. FR-MR-001.</summary>
public sealed record RoutingResult
{
    /// <summary>Selected model identifier (e.g. "gpt-4o-mini", "llama-3-8b").</summary>
    public string ModelId { get; init; } = string.Empty;

    /// <summary>Optional endpoint URL when different from default registry.</summary>
    public string? Endpoint { get; init; }

    /// <summary>Optional confidence score (0–1) when available from the tier.</summary>
    public double? Confidence { get; init; }

    /// <summary>Which tier or strategy selected this model (e.g. "cascade-tier-0").</summary>
    public string? Source { get; init; }

    /// <summary>Estimated cost for this request (when using cost-aware routing). MR-3.4.</summary>
    public decimal? EstimatedCost { get; init; }

    /// <summary>True when the current total tracked cost exceeds the configured budget. MR-3.4.</summary>
    public bool OverBudget { get; init; }
}
