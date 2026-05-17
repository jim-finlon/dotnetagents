namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Output of an <see cref="ISkillProjector"/> projection. Describes the target path, file name,
/// rendered contents, mode (write vs append-section), and any non-fatal warnings raised during
/// projection. Multiple projections for the same skill may exist (e.g. SKILL.md + sidecar
/// <c>agents/openai.yaml</c> for Codex when <c>invocation.modelInvokable=false</c>).
/// </summary>
/// <param name="TargetPath">
/// Relative path (from repo root or actor home, depending on <see cref="ProjectionContext.TargetScope"/>)
/// to the directory that should hold the projected file. For Cursor: <c>.cursor/skills/&lt;name&gt;</c>.
/// </param>
/// <param name="FileName">
/// File name inside <see cref="TargetPath"/>. Typically <c>SKILL.md</c>; may be a sidecar like
/// <c>openai.yaml</c> or <c>&lt;name&gt;.instructions.md</c>.
/// </param>
/// <param name="Contents">Rendered file contents (UTF-8 string).</param>
/// <param name="Mode">
/// Whether to write the file (idempotent overwrite when content hash differs) or to append a marker-bounded
/// section to an existing file (AGENTS.md, .github/copilot-instructions.md).
/// </param>
/// <param name="Warnings">Non-fatal warnings raised during projection (e.g. stripped superset fields).</param>
/// <param name="MarkerKey">
/// Story e399bfce (SUC Projection P1 follow-up): for
/// <see cref="SkillProjectionMode.AppendSection"/> projections only, the per-section key
/// used to compose the marker pair <c>&lt;!-- dna-skill:&lt;MarkerKey&gt;:begin --&gt;</c> …
/// <c>&lt;!-- dna-skill:&lt;MarkerKey&gt;:end --&gt;</c>. Typically the skill id. Ignored for
/// Write and Sidecar modes; <see cref="AtomicFileProjectionApplier"/> rejects AppendSection
/// projections that omit it.
/// </param>
public sealed record SkillProjection(
    string TargetPath,
    string FileName,
    string Contents,
    SkillProjectionMode Mode,
    IReadOnlyList<SkillProjectionWarning> Warnings,
    string? MarkerKey = null);

/// <summary>
/// Mode for a <see cref="SkillProjection"/>.
/// </summary>
public enum SkillProjectionMode
{
    /// <summary>Idempotent overwrite of the entire file at <see cref="SkillProjection.TargetPath"/>.</summary>
    Write,

    /// <summary>
    /// Append (or replace, idempotently) a comment-marker-bounded section inside an existing file.
    /// Used for AGENTS.md grafting and Copilot global-instructions.
    /// </summary>
    AppendSection,

    /// <summary>Sidecar file that lives alongside another projection (e.g. Codex <c>agents/openai.yaml</c>).</summary>
    Sidecar,
}

/// <summary>
/// Non-fatal warning raised by a projector (e.g. "stripped Claude-superset field <c>allowed-tools</c>
/// because target surface is Cursor").
/// </summary>
/// <param name="Code">Short machine-readable code (e.g. <c>stripped_superset_field</c>).</param>
/// <param name="Message">Human-readable warning text.</param>
public sealed record SkillProjectionWarning(string Code, string Message);
