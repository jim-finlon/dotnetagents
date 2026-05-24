// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.CounterAgents;

namespace DotNetAgents.Core.CounterAgents;

/// <summary>
/// Default <see cref="ICounterAgentVerdictAggregator"/> implementing the documented precedence:
/// any Block produces Block; otherwise any Concern produces Concern; otherwise Approve.
/// Among same-kind verdicts, the highest <see cref="CounterAgentSeverity"/> is reported and
/// reasons are flattened in severity-desc order.
/// </summary>
public sealed class CounterAgentVerdictAggregator : ICounterAgentVerdictAggregator
{
    /// <inheritdoc />
    public CounterAgentAggregateVerdict Aggregate(
        IReadOnlyList<CounterAgentVerdict> verdicts,
        DateTimeOffset? evaluatedAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(verdicts);

        var evaluatedAt = evaluatedAtUtc ?? DateTimeOffset.UtcNow;

        if (verdicts.Count == 0)
        {
            return new CounterAgentAggregateVerdict(
                Kind: CounterAgentVerdictKind.Approve,
                HighestSeverity: CounterAgentSeverity.Trivial,
                AllVerdicts: Array.Empty<CounterAgentVerdict>(),
                CombinedReasons: Array.Empty<string>(),
                EvaluatedAtUtc: evaluatedAt);
        }

        var hasBlock = verdicts.Any(v => v.Kind == CounterAgentVerdictKind.Block);
        var hasConcern = verdicts.Any(v => v.Kind == CounterAgentVerdictKind.Concern);

        var aggregateKind = hasBlock
            ? CounterAgentVerdictKind.Block
            : (hasConcern ? CounterAgentVerdictKind.Concern : CounterAgentVerdictKind.Approve);

        var nonApprove = verdicts.Where(v => v.Kind != CounterAgentVerdictKind.Approve).ToList();

        var highestSeverity = nonApprove.Count == 0
            ? CounterAgentSeverity.Trivial
            : nonApprove.Max(v => v.Severity);

        var combinedReasons = nonApprove
            .OrderByDescending(v => v.Severity)
            .ThenBy(v => v.CounterAgentId, StringComparer.Ordinal)
            .SelectMany(v => v.Reasons.Select(r => $"[{v.CounterAgentId}] {r}"))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new CounterAgentAggregateVerdict(
            Kind: aggregateKind,
            HighestSeverity: highestSeverity,
            AllVerdicts: verdicts.ToArray(),
            CombinedReasons: combinedReasons,
            EvaluatedAtUtc: evaluatedAt);
    }
}
