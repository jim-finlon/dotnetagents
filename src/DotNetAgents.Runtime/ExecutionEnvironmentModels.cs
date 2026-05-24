// SPDX-License-Identifier: Apache-2.0

using System.Collections.ObjectModel;

namespace DotNetAgents.Runtime;

public enum ExecutionEnvironmentKind
{
    LocalWorktree,
    Container,
    RemoteSsh,
    KubernetesJob,
    CloudSandbox,
    Fake
}

public enum ExecutionBlastRadius
{
    CurrentProcess,
    WorktreePath,
    Container,
    RemoteHost,
    ClusterNamespace,
    CloudAccount
}

public enum ExecutionPersistenceMode
{
    Ephemeral,
    Worktree,
    DurableVolume
}

public enum ExecutionCredentialMode
{
    None,
    ActorScoped,
    WorkerLease,
    InheritedEnvironment
}

public enum ExecutionNetworkMode
{
    Disabled,
    LocalhostOnly,
    RestrictedEgress,
    Full
}

public enum ExecutionCleanupGuarantee
{
    BestEffort,
    VerifiedPathScoped,
    ProviderManaged
}

public enum ExecutionApprovalRequirement
{
    None,
    StoryClaimRequired,
    HumanApproval,
    PrivilegedOperator
}

public enum CommandExecutionStatus
{
    Succeeded,
    Failed,
    TimedOut,
    Cancelled
}

public sealed record ExecutionEnvironmentProviderMetadata(
    string ProviderName,
    ExecutionEnvironmentKind Kind,
    IReadOnlySet<string> CapabilityTags,
    ExecutionBlastRadius BlastRadius,
    ExecutionPersistenceMode PersistenceMode,
    ExecutionCredentialMode CredentialMode,
    ExecutionNetworkMode NetworkMode,
    ExecutionCleanupGuarantee CleanupGuarantee,
    ExecutionApprovalRequirement ApprovalRequirement);

public sealed record ExecutionLeaseRequest
{
    public string ActorId { get; init; } = string.Empty;
    public string Purpose { get; init; } = string.Empty;
    public string BaseCommit { get; init; } = string.Empty;
    public string? BranchName { get; init; }
    public string? RootPath { get; init; }
    public TimeSpan? TimeToLive { get; init; }
    public IReadOnlySet<string> AllowedOperations { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = ReadOnlyDictionary<string, string>.Empty;
}

public sealed record ExecutionLease : IExecutionLease
{
    public string LeaseId { get; init; } = Guid.NewGuid().ToString("n");
    public string ProviderName { get; init; } = string.Empty;
    public string ActorId { get; init; } = string.Empty;
    public string Purpose { get; init; } = string.Empty;
    public string BaseCommit { get; init; } = string.Empty;
    public string? BranchName { get; init; }
    public string RootPath { get; init; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAtUtc { get; init; }
    public IReadOnlySet<string> AllowedOperations { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = ReadOnlyDictionary<string, string>.Empty;
    public IReadOnlySet<string> CapabilityTags { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public ExecutionBlastRadius BlastRadius { get; init; }
    public ExecutionPersistenceMode PersistenceMode { get; init; }
    public ExecutionCredentialMode CredentialMode { get; init; }
    public ExecutionNetworkMode NetworkMode { get; init; }
    public ExecutionCleanupGuarantee CleanupGuarantee { get; init; }
    public ExecutionApprovalRequirement ApprovalRequirement { get; init; }
    public string? CleanupCommand { get; init; }
}

public sealed record CommandExecutionRequest(
    string LeaseId,
    string Command,
    IReadOnlyList<string>? Arguments = null,
    string? WorkingDirectory = null,
    TimeSpan? Timeout = null);

public sealed record CommandExecutionResult
{
    public string CommandId { get; init; } = Guid.NewGuid().ToString("n");
    public string LeaseId { get; init; } = string.Empty;
    public string CommandLine { get; init; } = string.Empty;
    public CommandExecutionStatus Status { get; init; }
    public int ExitCode { get; init; }
    public DateTimeOffset StartedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CompletedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public ArtifactReference? StandardOutputRef { get; init; }
    public ArtifactReference? StandardErrorRef { get; init; }
    public IReadOnlyList<ArtifactReference> ArtifactRefs { get; init; } = [];
    public string? ErrorMessage { get; init; }
}

public sealed record ArtifactCollectionRequest(
    string LeaseId,
    string RelativePath,
    string MediaType = "application/octet-stream");

public sealed record ArtifactCollectionResult(
    string LeaseId,
    ArtifactReference Artifact);

public sealed record ExecutionCleanupRequest(
    string LeaseId,
    string RequestedPath);

public sealed record CleanupPolicyDecision(
    bool Allowed,
    string Reason,
    IReadOnlyList<string>? RefusedPaths = null);

public sealed record ExecutionCleanupReceipt
{
    public string ReceiptId { get; init; } = Guid.NewGuid().ToString("n");
    public string LeaseId { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
    public string RequestedPath { get; init; } = string.Empty;
    public IReadOnlyList<string> RemovedPaths { get; init; } = [];
    public IReadOnlyList<string> RefusedPaths { get; init; } = [];
    public bool Succeeded { get; init; }
    public string? FailureReason { get; init; }
    public string? CleanupCommand { get; init; }
    public DateTimeOffset CleanedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
