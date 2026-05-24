// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Agents.IntentProjector;

/// <summary>
/// Story 2215c3b4 — projects an <see cref="IntentDocument"/> through a named
/// <see cref="SessionRoleProfile"/>. Operators or returning agents call
/// <see cref="Export"/> with a document, role id, and consumer id; the exporter
/// resolves the role's IncludeTags + default projection kind, then defers to
/// <see cref="IntentProjector"/> for the canonical projection.
/// </summary>
/// <remarks>
/// The exporter is a thin facade: zero new policy state, no parallel storage.
/// Switching roles on the same document is idempotent — call Export again with
/// a different role id, and the projector emits a different artifact set.
/// </remarks>
public sealed class SessionRoleExporter
{
    private readonly IntentProjector _projector;

    public SessionRoleExporter(IntentProjector? projector = null)
    {
        _projector = projector ?? new IntentProjector();
    }

    /// <summary>
    /// Export the document through the role profile identified by <paramref name="roleId"/>.
    /// The <paramref name="consumerId"/> picks the consumer the projector binds to (must
    /// exist in the document and support the role's default projection kind).
    /// <paramref name="projectionKindOverride"/> overrides the role's default projection
    /// kind when the caller wants a different shape (e.g. AGENTS.md instead of model prompt).
    /// <paramref name="targetRoot"/> opt-in materializes artifacts on disk.
    /// </summary>
    public SessionRoleExportReceipt Export(
        IntentDocument document,
        string roleId,
        string consumerId,
        IntentProjectionKind? projectionKindOverride = null,
        string? targetRoot = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (string.IsNullOrWhiteSpace(roleId))
            throw new ArgumentException("roleId is required.", nameof(roleId));
        if (string.IsNullOrWhiteSpace(consumerId))
            throw new ArgumentException("consumerId is required.", nameof(consumerId));

        var profile = SessionRoleCatalog.Get(roleId);
        var kind = projectionKindOverride ?? profile.DefaultProjectionKind;

        var request = new IntentProjectionRequest(
            Kind: kind,
            ConsumerId: consumerId.Trim(),
            TargetRoot: targetRoot,
            IncludeTags: profile.IncludeTags,
            IncludeReferenceBlocks: !profile.DropReferenceBlocks);

        var receipt = _projector.Project(document, request);
        return new SessionRoleExportReceipt(
            DocumentId: document.Id,
            RoleId: profile.Id,
            RoleDisplayName: profile.DisplayName,
            ConsumerId: receipt.ConsumerId,
            Kind: receipt.Kind,
            GeneratedAtUtc: receipt.GeneratedAtUtc,
            Artifacts: receipt.Artifacts,
            ValidationMessages: receipt.ValidationMessages,
            IncludeTags: profile.IncludeTags,
            DroppedReferenceBlocks: profile.DropReferenceBlocks);
    }

    /// <summary>List the role catalog in catalog order.</summary>
    public IReadOnlyCollection<SessionRoleProfile> ListAvailableRoles() => SessionRoleCatalog.All;
}

/// <summary>
/// Story 2215c3b4 — receipt returned by <see cref="SessionRoleExporter.Export"/>.
/// Carries the role identity + the underlying projection artifacts so the caller
/// can prove which role drove the export and feed the artifacts into a session
/// restart pipeline (agent runtime, CLI bootstrap, workflow control plane handoff).
/// </summary>
public sealed record SessionRoleExportReceipt(
    string DocumentId,
    string RoleId,
    string RoleDisplayName,
    string ConsumerId,
    IntentProjectionKind Kind,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<IntentProjectionArtifact> Artifacts,
    IReadOnlyList<string> ValidationMessages,
    IReadOnlyList<string> IncludeTags,
    bool DroppedReferenceBlocks);
