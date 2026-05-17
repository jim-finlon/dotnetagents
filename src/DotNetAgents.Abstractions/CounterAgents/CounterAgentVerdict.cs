namespace DotNetAgents.Abstractions.CounterAgents;

/// <summary>
/// A single counter-agent's verdict on a proposed action.
/// </summary>
/// <remarks>
/// Verdicts are append-only evidence. The aggregator combines multiple verdicts into a final
/// <see cref="CounterAgentAggregateVerdict"/> via priority rules: <see cref="CounterAgentVerdictKind.Block"/>
/// dominates <see cref="CounterAgentVerdictKind.Concern"/> dominates <see cref="CounterAgentVerdictKind.Approve"/>;
/// among same-kind verdicts the highest <see cref="Severity"/> wins.
/// </remarks>
public sealed record CounterAgentVerdict
{
    /// <summary>Stable id of the counter-agent that issued this verdict.</summary>
    public required string CounterAgentId { get; init; }

    /// <summary>The verdict kind: Approve / Concern / Block.</summary>
    public required CounterAgentVerdictKind Kind { get; init; }

    /// <summary>The verdict severity. Approve verdicts use <see cref="CounterAgentSeverity.Trivial"/>; Block defaults to <see cref="CounterAgentSeverity.Critical"/>.</summary>
    public required CounterAgentSeverity Severity { get; init; }

    /// <summary>Concrete reasons supporting the verdict. Each entry is operator-readable; cite specifics not vague disapproval.</summary>
    public required IReadOnlyList<string> Reasons { get; init; }

    /// <summary>Optional evidence references (path:line, story id, deployment id, etc.). Used by dashboards to deep-link.</summary>
    public IReadOnlyList<string> EvidenceRefs { get; init; } = Array.Empty<string>();

    /// <summary>When the verdict was decided (UTC).</summary>
    public required DateTimeOffset DecidedAtUtc { get; init; }

    /// <summary>Optional structured metadata the counter-agent attached (e.g. cost estimate, posture).</summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>Convenience factory for an Approve verdict (severity Trivial, no reasons).</summary>
    public static CounterAgentVerdict Approve(string counterAgentId, DateTimeOffset? decidedAtUtc = null) => new()
    {
        CounterAgentId = counterAgentId,
        Kind = CounterAgentVerdictKind.Approve,
        Severity = CounterAgentSeverity.Trivial,
        Reasons = Array.Empty<string>(),
        DecidedAtUtc = decidedAtUtc ?? DateTimeOffset.UtcNow,
    };

    /// <summary>Convenience factory for a Concern verdict.</summary>
    public static CounterAgentVerdict Concern(
        string counterAgentId,
        IEnumerable<string> reasons,
        CounterAgentSeverity severity = CounterAgentSeverity.Minor,
        IEnumerable<string>? evidenceRefs = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        DateTimeOffset? decidedAtUtc = null) => new()
    {
        CounterAgentId = counterAgentId,
        Kind = CounterAgentVerdictKind.Concern,
        Severity = severity,
        Reasons = reasons.ToArray(),
        EvidenceRefs = evidenceRefs?.ToArray() ?? Array.Empty<string>(),
        DecidedAtUtc = decidedAtUtc ?? DateTimeOffset.UtcNow,
        Metadata = metadata,
    };

    /// <summary>Convenience factory for a Block verdict (severity defaults to Critical).</summary>
    public static CounterAgentVerdict Block(
        string counterAgentId,
        IEnumerable<string> reasons,
        CounterAgentSeverity severity = CounterAgentSeverity.Critical,
        IEnumerable<string>? evidenceRefs = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        DateTimeOffset? decidedAtUtc = null) => new()
    {
        CounterAgentId = counterAgentId,
        Kind = CounterAgentVerdictKind.Block,
        Severity = severity,
        Reasons = reasons.ToArray(),
        EvidenceRefs = evidenceRefs?.ToArray() ?? Array.Empty<string>(),
        DecidedAtUtc = decidedAtUtc ?? DateTimeOffset.UtcNow,
        Metadata = metadata,
    };
}
