namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Canonical skill manifest sourced from <c>dna-skills/&lt;domain&gt;/&lt;name&gt;/SKILL.md</c>.
/// Holds the raw frontmatter text + body and exposes a minimal projection-time view.
/// </summary>
/// <remarks>
/// Phase 0 (story SUC-03): the manifest carries the original SKILL.md as-is so the Cursor
/// projector ships a byte-identical file. Later stories add structured access to frontmatter
/// (id / version / surfaces / posture / etc.) for the lossy-edge projectors. Until then,
/// projectors that need superset-field stripping look the fields up by scanning the
/// <see cref="FrontmatterRaw"/> string. The schema contract is <c>docs/schemas/dna.skill.v1.schema.json</c>
/// shipped under story SUC-01.
/// </remarks>
/// <param name="Name">Skill <c>name</c> from frontmatter (kebab-case slug).</param>
/// <param name="FrontmatterRaw">Raw YAML frontmatter text (between the opening and closing <c>---</c>).</param>
/// <param name="Body">Markdown body (everything after the closing <c>---</c> line).</param>
/// <param name="CanonicalDirectory">
/// Absolute filesystem path to the canonical skill directory (the parent of <c>SKILL.md</c>).
/// Projectors that emit sibling files (scripts, references, assets) read from this path.
/// </param>
public sealed record SkillManifest(
    string Name,
    string FrontmatterRaw,
    string Body,
    string CanonicalDirectory);
