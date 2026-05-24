// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Abstractions.CounterAgents;

/// <summary>
/// The aggregate of N counter-agents' individual verdicts on a single proposed action.
/// Produced by <see cref="ICounterAgentVerdictAggregator"/> and consumed by the middleware to
/// decide whether the action proceeds, attaches concerns as metadata, or is halted.
/// </summary>
/// <param name="Kind">The aggregate verdict kind. Block dominates Concern dominates Approve.</param>
/// <param name="HighestSeverity">The highest <see cref="CounterAgentSeverity"/> across all non-Approve verdicts. Used by dashboards for alert ranking.</param>
/// <param name="AllVerdicts">Every verdict that contributed to the aggregate, append-only. Includes Approve verdicts so dashboards can show which counter-agents reviewed.</param>
/// <param name="CombinedReasons">Flattened, deduplicated reasons from all non-Approve verdicts, ordered by severity-desc then counter-agent id for stability.</param>
/// <param name="EvaluatedAtUtc">When the aggregation occurred.</param>
public sealed record CounterAgentAggregateVerdict(
    CounterAgentVerdictKind Kind,
    CounterAgentSeverity HighestSeverity,
    IReadOnlyList<CounterAgentVerdict> AllVerdicts,
    IReadOnlyList<string> CombinedReasons,
    DateTimeOffset EvaluatedAtUtc)
{
    /// <summary>True when <see cref="Kind"/> is <see cref="CounterAgentVerdictKind.Block"/>.</summary>
    public bool IsBlocked => Kind == CounterAgentVerdictKind.Block;

    /// <summary>True when any non-Approve verdict was issued (Concern or Block).</summary>
    public bool HasConcerns => Kind != CounterAgentVerdictKind.Approve;

    /// <summary>The subset of <see cref="AllVerdicts"/> that were Block. Empty when not blocked.</summary>
    public IEnumerable<CounterAgentVerdict> BlockingVerdicts => AllVerdicts.Where(v => v.Kind == CounterAgentVerdictKind.Block);
}

/// <summary>
/// Aggregates individual counter-agent verdicts into a final <see cref="CounterAgentAggregateVerdict"/>.
/// Default implementation is severity-ordered Block&gt;Concern&gt;Approve precedence.
/// </summary>
public interface ICounterAgentVerdictAggregator
{
    CounterAgentAggregateVerdict Aggregate(
        IReadOnlyList<CounterAgentVerdict> verdicts,
        DateTimeOffset? evaluatedAtUtc = null);
}
