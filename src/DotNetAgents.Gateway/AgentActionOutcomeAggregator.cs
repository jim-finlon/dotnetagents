// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Gateway;

/// <summary>
/// Aggregates outcome receipts into per-(agent, domain, task type, model, prompt version, cost
/// tier, local-vs-external escalation reason) score rollups. Story 8bd0eb13 — surface that
/// PromptSpecialist + Model Gateway routing policy + dashboards consume to evaluate not just
/// what model was used but whether that model produced good work for that domain at an
/// acceptable cost and latency.
/// </summary>
public interface IAgentActionOutcomeAggregator
{
    /// <summary>Compute aggregated scores over the result set returned by the supplied query.</summary>
    AgentActionOutcomeAggregate Aggregate(OutcomeQuery query);
}

/// <summary>Aggregated rollup over a query window. <see cref="SampleCount"/> = 0 when no recorded outcomes match.</summary>
/// <param name="SampleCount">Total receipts that contributed; 0 = no signal.</param>
/// <param name="UnknownCount">Receipts in <see cref="OutcomeKind.Unknown"/> state — present in the window but with no consequence signal. Distinguished from low-score receipts.</param>
/// <param name="PendingCount">Receipts in <see cref="OutcomeKind.Pending"/> state.</param>
/// <param name="AverageCorrectness">Mean Correctness across receipts that recorded the dimension; null when no receipts recorded it.</param>
/// <param name="AverageAcceptanceCriteriaSatisfaction">Mean AcceptanceCriteriaSatisfaction; null when not recorded.</param>
/// <param name="AverageTestPassRatio">Mean TestPassRatio; null when not recorded.</param>
/// <param name="AverageLatencyMs">Mean LatencyMs; null when not recorded.</param>
/// <param name="TotalCostUsd">Sum of CostUsd across receipts.</param>
/// <param name="AverageReworkCount">Mean ReworkCount across receipts.</param>
/// <param name="AverageOperatorSatisfaction">Mean operator satisfaction; null when no receipts collected it.</param>
/// <param name="ReviewVerdictBreakdown">Counts keyed by verdict string ("Approved", "ChangesRequested", "Blocked"); empty when no receipts recorded a verdict.</param>
/// <param name="LocalRouteCount">Receipts where the route was local.</param>
/// <param name="ExternalRouteCount">Receipts where the route escalated.</param>
/// <param name="EscalationReasonCounts">Counts keyed by EscalationReason; empty when no escalations.</param>
/// <param name="SafetyEventCount">Total safety events across receipts.</param>
public sealed record AgentActionOutcomeAggregate(
    int SampleCount,
    int UnknownCount,
    int PendingCount,
    double? AverageCorrectness,
    double? AverageAcceptanceCriteriaSatisfaction,
    double? AverageTestPassRatio,
    double? AverageLatencyMs,
    decimal TotalCostUsd,
    double AverageReworkCount,
    double? AverageOperatorSatisfaction,
    IReadOnlyDictionary<string, int> ReviewVerdictBreakdown,
    int LocalRouteCount,
    int ExternalRouteCount,
    IReadOnlyDictionary<string, int> EscalationReasonCounts,
    int SafetyEventCount)
{
    public static AgentActionOutcomeAggregate Empty { get; } = new(
        0, 0, 0, null, null, null, null, 0m, 0, null,
        new Dictionary<string, int>(), 0, 0, new Dictionary<string, int>(), 0);
}

/// <summary>Default aggregator backed by an <see cref="IAgentActionOutcomeStore"/>.</summary>
public sealed class DefaultAgentActionOutcomeAggregator : IAgentActionOutcomeAggregator
{
    private readonly IAgentActionOutcomeStore _store;

    public DefaultAgentActionOutcomeAggregator(IAgentActionOutcomeStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public AgentActionOutcomeAggregate Aggregate(OutcomeQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var rows = _store.Query(query);
        if (rows.Count == 0) return AgentActionOutcomeAggregate.Empty;

        // Recorded receipts contribute to the dimensional averages; Pending/Unknown only
        // contribute to the count fields so dashboards don't conflate "no data" with "bad result".
        var recorded = rows.Where(o => o.Kind == OutcomeKind.Recorded).ToArray();

        double? Avg(Func<AgentActionOutcome, double?> selector)
        {
            var values = recorded.Select(selector).Where(v => v.HasValue).Select(v => v!.Value).ToArray();
            return values.Length == 0 ? null : values.Average();
        }
        double? AvgLong(Func<AgentActionOutcome, long?> selector)
        {
            var values = recorded.Select(selector).Where(v => v.HasValue).Select(v => (double)v!.Value).ToArray();
            return values.Length == 0 ? null : values.Average();
        }

        var verdictBreakdown = recorded
            .Where(o => !string.IsNullOrEmpty(o.Scores.ReviewVerdict))
            .GroupBy(o => o.Scores.ReviewVerdict!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var escalationCounts = rows
            .Where(o => !o.WasLocalRoute && !string.IsNullOrEmpty(o.EscalationReason))
            .GroupBy(o => o.EscalationReason!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        return new AgentActionOutcomeAggregate(
            SampleCount: rows.Count,
            UnknownCount: rows.Count(o => o.Kind == OutcomeKind.Unknown),
            PendingCount: rows.Count(o => o.Kind == OutcomeKind.Pending),
            AverageCorrectness: Avg(o => o.Scores.Correctness),
            AverageAcceptanceCriteriaSatisfaction: Avg(o => o.Scores.AcceptanceCriteriaSatisfaction),
            AverageTestPassRatio: Avg(o => o.Scores.TestPassRatio),
            AverageLatencyMs: AvgLong(o => o.Scores.LatencyMs),
            TotalCostUsd: rows.Sum(o => o.Scores.CostUsd ?? 0m),
            AverageReworkCount: rows.Average(o => (double)o.ReworkCount),
            AverageOperatorSatisfaction: Avg(o => o.OperatorSatisfaction),
            ReviewVerdictBreakdown: verdictBreakdown,
            LocalRouteCount: rows.Count(o => o.WasLocalRoute),
            ExternalRouteCount: rows.Count(o => !o.WasLocalRoute),
            EscalationReasonCounts: escalationCounts,
            SafetyEventCount: rows.Sum(o => o.SafetyEvents.Count));
    }
}
