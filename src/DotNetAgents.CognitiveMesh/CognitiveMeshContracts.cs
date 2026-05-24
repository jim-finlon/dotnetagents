// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.CognitiveMesh;

/// <summary>
/// Top-level traversal ledger entry for a Cognitive Mesh claim. Stores operational evidence and
/// rationale summaries only; it must not contain raw chain-of-thought, secrets, or customer data.
/// </summary>
public sealed record TraversalRecord
{
    public const string CurrentSchemaVersion = "dna.cognitive_mesh.traversal_record.v1";

    public required string SchemaVersion { get; init; } = CurrentSchemaVersion;
    public required string TraversalId { get; init; }
    public required string SubjectRef { get; init; }
    public required TraversalStatus Status { get; init; }
    public required DateTimeOffset CapturedAtUtc { get; init; }
    public required string CapturedByActorId { get; init; }
    public required SemanticClaim Claim { get; init; }
    public IReadOnlyList<EvidenceRef> EvidenceRefs { get; init; } = Array.Empty<EvidenceRef>();
    public IReadOnlyList<ResidueRecord> Residues { get; init; } = Array.Empty<ResidueRecord>();
    public IReadOnlyList<BridgeAttempt> BridgeAttempts { get; init; } = Array.Empty<BridgeAttempt>();
    public JuryVerdict? JuryVerdict { get; init; }
    public CollapseDecision? CollapseDecision { get; init; }
    public ClaimWarrantStatus WarrantStatus { get; init; } = ClaimWarrantStatus.Captured;
    public string? Summary { get; init; }
}

/// <summary>
/// A scoped proposition being carried through the Cognitive Mesh. Rationale is a short summary for
/// auditability, not hidden model reasoning.
/// </summary>
public sealed record SemanticClaim
{
    public required string ClaimId { get; init; }
    public required string Scope { get; init; }
    public required string Statement { get; init; }
    public string? RationaleSummary { get; init; }
    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Reference to external evidence by URI, artifact id, story id, commit, or similar durable handle.
/// </summary>
public sealed record EvidenceRef
{
    public required string EvidenceId { get; init; }
    public required string Kind { get; init; }
    public required string Ref { get; init; }
    public string? Summary { get; init; }
    public DateTimeOffset? ObservedAtUtc { get; init; }
}

/// <summary>
/// Captures unresolved contradiction, ambiguity, or context loss that must be preserved across
/// collapse decisions without exposing restricted payload material.
/// </summary>
public sealed record ResidueRecord
{
    public required string ResidueId { get; init; }
    public required string Description { get; init; }
    public ResidueDisposition Disposition { get; init; } = ResidueDisposition.Unresolved;
    public string? OwnerActorId { get; init; }
}

/// <summary>
/// Records a bounded attempt to connect a claim with an adjacent scope, artifact, or warrant.
/// </summary>
public sealed record BridgeAttempt
{
    public required string BridgeId { get; init; }
    public required string SourceScope { get; init; }
    public required string TargetScope { get; init; }
    public required string Outcome { get; init; }
    public string? Summary { get; init; }
}

/// <summary>
/// Deterministic or jury-mediated support verdict for a scoped claim.
/// </summary>
public sealed record JuryVerdict
{
    public required string VerdictId { get; init; }
    public required bool Supported { get; init; }
    public required decimal Confidence { get; init; }
    public required DateTimeOffset DecidedAtUtc { get; init; }
    public IReadOnlyList<string> ReviewerActorIds { get; init; } = Array.Empty<string>();
    public string? Summary { get; init; }
}

/// <summary>
/// Decision that collapses a traversal into a committed, rejected, superseded, or deferred posture.
/// </summary>
public sealed record CollapseDecision
{
    public required string DecisionId { get; init; }
    public required ClaimWarrantStatus ResultStatus { get; init; }
    public required DateTimeOffset DecidedAtUtc { get; init; }
    public required string DecidedByActorId { get; init; }
    public string? Summary { get; init; }
}

public enum ClaimWarrantStatus
{
    Captured,
    Held,
    InternallyConsistent,
    EvidenceLinked,
    JurySupported,
    ExternallyCoupled,
    OperationallyCommitted,
    Superseded,
    Falsified,
    DisciplinedDeparture
}

public enum ResidueDisposition
{
    Unresolved,
    AcceptedRisk,
    Deferred,
    Resolved,
    Superseded
}

public enum TraversalStatus
{
    Open,
    AwaitingEvidence,
    AwaitingJury,
    Collapsed,
    Reopened,
    Closed
}
