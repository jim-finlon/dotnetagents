// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Observability.Metrics;

/// <summary>
/// Represents a summary of costs over a time period.
/// </summary>
public record CostSummary
{
    /// <summary>
    /// Gets the total cost in USD.
    /// </summary>
    public decimal TotalCost { get; init; }

    /// <summary>
    /// Gets the total number of LLM calls.
    /// </summary>
    public int TotalCalls { get; init; }

    /// <summary>
    /// Gets the total number of input tokens.
    /// </summary>
    public long TotalInputTokens { get; init; }

    /// <summary>
    /// Gets the total number of output tokens.
    /// </summary>
    public long TotalOutputTokens { get; init; }

    /// <summary>
    /// Gets the time period this summary covers.
    /// </summary>
    public TimeSpan Period { get; init; }

    /// <summary>
    /// Gets the start time of the period.
    /// </summary>
    public DateTime StartTime { get; init; }

    /// <summary>
    /// Gets the end time of the period.
    /// </summary>
    public DateTime EndTime { get; init; }

    /// <summary>
    /// Gets the cost breakdown by model.
    /// </summary>
    public IReadOnlyDictionary<string, decimal> CostByModel { get; init; } = new Dictionary<string, decimal>();
}
