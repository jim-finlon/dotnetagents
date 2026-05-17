using System.Text.Json.Serialization;

namespace DotNetAgents.Skills;

/// <summary>
/// Typed representation of a <c>dna.skill.capability-pack.v1</c> document. The schema lives at
/// <c>docs/schemas/dna.skill.capability-pack.v1.schema.json</c>; this record exists so the
/// emitter and channel-manifest writer share a single deterministic shape.
/// </summary>
/// <param name="SchemaVersion">Always <c>dna.skill.capability-pack.v1</c>.</param>
/// <param name="PackKind">Pack kind (skill | hook | prompt | policy | eval-pack | composite).</param>
/// <param name="Contents">One entry per canonical content artifact in the pack.</param>
/// <param name="ClientCompatibility">One entry per (client, emitter) the projection orchestrator ran.</param>
/// <param name="RolloutChannel">Catalog routing channel.</param>
/// <param name="ApprovalState">Lifecycle state for the pack.</param>
/// <param name="Scoring">Optional scoring snapshot.</param>
/// <param name="Provenance">Optional provenance (sourceRepoRef, extractedFromLessonRef). signedBy[] tracked separately.</param>
public sealed record CapabilityPack(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("packKind")] CapabilityPackKind PackKind,
    [property: JsonPropertyName("contents")] IReadOnlyList<CapabilityPackContent> Contents,
    [property: JsonPropertyName("clientCompatibility")] IReadOnlyList<CapabilityPackClientCompatibility> ClientCompatibility,
    [property: JsonPropertyName("rolloutChannel")] CapabilityPackChannel RolloutChannel,
    [property: JsonPropertyName("approvalState")] CapabilityPackApprovalState ApprovalState,
    [property: JsonPropertyName("scoring")] CapabilityPackScoring? Scoring = null,
    [property: JsonPropertyName("provenance")] CapabilityPackProvenance? Provenance = null);

/// <summary>One content entry inside a capability pack.</summary>
/// <param name="ContentId">Stable id for this content (skill name, hook id, etc.).</param>
/// <param name="ContentKind">Kind of artifact.</param>
/// <param name="Version">SemVer 2.0.0-compatible version string.</param>
/// <param name="Checksum">Canonical <c>sha256:&lt;hex&gt;</c> checksum of the underlying body.</param>
/// <param name="Location">Repo-relative or URL location where the canonical artifact lives.</param>
public sealed record CapabilityPackContent(
    [property: JsonPropertyName("contentId")] string ContentId,
    [property: JsonPropertyName("contentKind")] CapabilityPackContentKind ContentKind,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("checksum")] string Checksum,
    [property: JsonPropertyName("location")] string Location);

/// <summary>One per-client compatibility record.</summary>
/// <param name="Client">Schema-defined client id (codex | cursor | claude-code | claude-desktop | copilot | goose | internal-dna).</param>
/// <param name="Emitter">Projector or orchestrator emitter id (e.g. ISkillProjector.ClientKind).</param>
/// <param name="Verified">Whether the orchestrator verified the projection succeeded byte-deterministically.</param>
/// <param name="MinClientVersion">Optional inclusive minimum vendor version (SemVer).</param>
/// <param name="MaxClientVersion">Optional inclusive maximum vendor version (SemVer).</param>
/// <param name="VerifiedAtUtc">Required when <see cref="Verified"/> is true.</param>
/// <param name="Notes">Optional free-form note from the orchestrator (e.g. warnings count).</param>
public sealed record CapabilityPackClientCompatibility(
    [property: JsonPropertyName("client")] string Client,
    [property: JsonPropertyName("emitter")] string Emitter,
    [property: JsonPropertyName("verified")] bool Verified,
    [property: JsonPropertyName("minClientVersion")] string? MinClientVersion = null,
    [property: JsonPropertyName("maxClientVersion")] string? MaxClientVersion = null,
    [property: JsonPropertyName("verifiedAtUtc")] string? VerifiedAtUtc = null,
    [property: JsonPropertyName("notes")] string? Notes = null);

/// <summary>Optional scoring snapshot from the registry.</summary>
public sealed record CapabilityPackScoring(
    [property: JsonPropertyName("usageCount")] int? UsageCount = null,
    [property: JsonPropertyName("evalScore")] double? EvalScore = null,
    [property: JsonPropertyName("operatorRating")] double? OperatorRating = null,
    [property: JsonPropertyName("lastScoredAtUtc")] string? LastScoredAtUtc = null);

/// <summary>Optional provenance block.</summary>
/// <param name="SourceRepoRef">Optional repo ref the pack was scored from.</param>
/// <param name="ExtractedFromLessonRef">Optional KnowledgeMemory lesson id used as the extraction source.</param>
/// <param name="SignedBy">Per-signer entries; populated by <see cref="CapabilityPackSigner.Sign"/>.</param>
public sealed record CapabilityPackProvenance(
    [property: JsonPropertyName("sourceRepoRef")] string? SourceRepoRef = null,
    [property: JsonPropertyName("extractedFromLessonRef")] string? ExtractedFromLessonRef = null,
    [property: JsonPropertyName("signedBy")] IReadOnlyList<CapabilityPackSignature>? SignedBy = null);

/// <summary>One <c>provenance.signedBy[]</c> entry per the schema.</summary>
/// <param name="ActorId">Stable actor id (e.g. <c>agent-alpha</c>).</param>
/// <param name="Alg">Algorithm identifier (<c>ed25519</c> | <c>rsa-pss-sha256</c>).</param>
/// <param name="KeyRef">Opaque key reference resolved by the signing key custodian.</param>
/// <param name="Signature">Base64-encoded signature bytes (schema regex <c>^[A-Za-z0-9+/]+=*$</c>).</param>
/// <param name="SignedAtUtc">ISO-8601 UTC timestamp.</param>
public sealed record CapabilityPackSignature(
    [property: JsonPropertyName("actorId")] string ActorId,
    [property: JsonPropertyName("alg")] string Alg,
    [property: JsonPropertyName("keyRef")] string KeyRef,
    [property: JsonPropertyName("signature")] string Signature,
    [property: JsonPropertyName("signedAtUtc")] string SignedAtUtc);

/// <summary>Pack kinds defined by the schema.</summary>
public enum CapabilityPackKind { Skill, Hook, Prompt, Policy, EvalPack, Composite }

/// <summary>Content kinds defined by the schema.</summary>
public enum CapabilityPackContentKind { Skill, Hook, Prompt, Policy, EvalPack }

/// <summary>Channels defined by the schema.</summary>
public enum CapabilityPackChannel { InternalOnly, Experimental, Stable, Deprecated }

/// <summary>Approval states defined by the schema.</summary>
public enum CapabilityPackApprovalState { Draft, Submitted, Validated, Approved, Published, Deprecated, Revoked }
