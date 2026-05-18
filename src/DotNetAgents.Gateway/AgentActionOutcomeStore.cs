using System.Collections.Concurrent;

namespace DotNetAgents.Gateway;

/// <summary>
/// Append-only outcome receipt store. Story 8bd0eb13 — backs both the recorder seam (where
/// workflow completion / review / test flows attach receipts) and the aggregator (where
/// PromptSpecialist + Model Gateway query outcomes by agent / domain / model / route). The
/// in-memory default is single-process; future stories may swap a Postgres or knowledge-memory service-backed
/// implementation without touching call sites.
/// </summary>
public interface IAgentActionOutcomeStore
{
    /// <summary>Record a new outcome receipt; throws on duplicate <see cref="AgentActionOutcome.OutcomeId"/>.</summary>
    void Record(AgentActionOutcome outcome);

    /// <summary>List every receipt linked to a model invocation id (multiple receipts per invocation are normal — completion + review + test).</summary>
    IReadOnlyList<AgentActionOutcome> GetByInvocation(Guid modelInvocationId);

    /// <summary>Filter receipts by an arbitrary subset of dimensions. All fields are optional; pass null to skip a filter.</summary>
    IReadOnlyList<AgentActionOutcome> Query(OutcomeQuery query);
}

/// <summary>Filter applied by <see cref="IAgentActionOutcomeStore.Query"/> + <see cref="IAgentActionOutcomeAggregator.Aggregate"/>.</summary>
/// <param name="AgentId">Optional agent filter.</param>
/// <param name="DomainTag">Optional domain filter.</param>
/// <param name="ModelId">Optional model filter.</param>
/// <param name="GatewayId">Optional gateway filter.</param>
/// <param name="WasLocalRoute">Optional local/external filter.</param>
/// <param name="EscalationReason">Optional escalation-reason filter (only meaningful when <see cref="WasLocalRoute"/> is false).</param>
/// <param name="Kind">Optional outcome-kind filter (default null = all kinds).</param>
/// <param name="LinkedArtifactType">Optional artifact-type filter.</param>
/// <param name="ObservedAtFromUtc">Optional inclusive lower bound on ObservedAtUtc.</param>
/// <param name="ObservedAtToUtc">Optional exclusive upper bound on ObservedAtUtc.</param>
public sealed record OutcomeQuery(
    string? AgentId = null,
    string? DomainTag = null,
    string? ModelId = null,
    string? GatewayId = null,
    bool? WasLocalRoute = null,
    string? EscalationReason = null,
    OutcomeKind? Kind = null,
    OutcomeArtifactType? LinkedArtifactType = null,
    DateTimeOffset? ObservedAtFromUtc = null,
    DateTimeOffset? ObservedAtToUtc = null);

/// <summary>Default in-memory store. Thread-safe via <see cref="ConcurrentDictionary{TKey,TValue}"/>.</summary>
public sealed class InMemoryAgentActionOutcomeStore : IAgentActionOutcomeStore
{
    private readonly ConcurrentDictionary<Guid, AgentActionOutcome> _byOutcomeId = new();

    public void Record(AgentActionOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        if (!_byOutcomeId.TryAdd(outcome.OutcomeId, outcome))
            throw new InvalidOperationException($"Duplicate outcome id {outcome.OutcomeId:N}.");
    }

    public IReadOnlyList<AgentActionOutcome> GetByInvocation(Guid modelInvocationId)
        => _byOutcomeId.Values.Where(o => o.ModelInvocationId == modelInvocationId).ToArray();

    public IReadOnlyList<AgentActionOutcome> Query(OutcomeQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return _byOutcomeId.Values.Where(o => Matches(o, query)).ToArray();
    }

    private static bool Matches(AgentActionOutcome o, OutcomeQuery q)
    {
        if (q.AgentId is not null && !string.Equals(o.AgentId, q.AgentId, StringComparison.OrdinalIgnoreCase)) return false;
        if (q.DomainTag is not null && !string.Equals(o.DomainTag, q.DomainTag, StringComparison.OrdinalIgnoreCase)) return false;
        if (q.ModelId is not null && !string.Equals(o.ModelId, q.ModelId, StringComparison.OrdinalIgnoreCase)) return false;
        if (q.GatewayId is not null && !string.Equals(o.GatewayId, q.GatewayId, StringComparison.OrdinalIgnoreCase)) return false;
        if (q.WasLocalRoute is { } local && o.WasLocalRoute != local) return false;
        if (q.EscalationReason is not null && !string.Equals(o.EscalationReason, q.EscalationReason, StringComparison.OrdinalIgnoreCase)) return false;
        if (q.Kind is { } kind && o.Kind != kind) return false;
        if (q.LinkedArtifactType is { } artifact && o.LinkedArtifactType != artifact) return false;
        if (q.ObservedAtFromUtc is { } from && o.ObservedAtUtc < from) return false;
        if (q.ObservedAtToUtc is { } to && o.ObservedAtUtc >= to) return false;
        return true;
    }
}
