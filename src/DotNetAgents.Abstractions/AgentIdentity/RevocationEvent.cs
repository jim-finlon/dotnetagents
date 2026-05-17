namespace DotNetAgents.Abstractions.AgentIdentity;

/// <summary>
/// Canonical revocation event propagated from CredentialsAgent to runtime services.
///
/// Story 4b618e27 — `RevokedByActorId` / `RevokedByActorType` are init-only
/// nullable properties (not positional) so existing callers that construct via
/// the positional ctor (DotNetAgents.Credentials.Client, tests) keep compiling.
/// CredentialsAgent service-side enforcement now requires the revoking actor on
/// every mutating revoke call; runtime consumers SHOULD read the fields when
/// reasoning about a revocation's provenance.
/// </summary>
public sealed record RevocationEvent(
    long Sequence,
    string EventId,
    string EventType,
    string SubjectId,
    string? AgentId,
    string? AgentInstanceId,
    string? WorkOrderId,
    string? AuthLeaseId,
    string? Reason,
    DateTimeOffset RevokedAtUtc)
{
    public string? RevokedByActorId { get; init; }
    public string? RevokedByActorType { get; init; }
}

public static class RevocationEventTypes
{
    public const string LeaseRevocation = "lease_revocation";
    public const string InstanceRevocation = "instance_revocation";
    public const string WorkOrderRevocation = "work_order_revocation";
}
