namespace DotNetAgents.Abstractions.AgentIdentity;

public sealed record ProvenanceBundle(
    string BundleId,
    string SupervisorRef,
    string? AgentInstanceId,
    string WorkOrderId,
    string LeaseGraphSnapshotJson,
    string? SandboxReceiptId,
    string? InstanceCardHash,
    DateTimeOffset CreatedAtUtc,
    string? SupersedesBundleId = null,
    string? SupersessionReason = null);
