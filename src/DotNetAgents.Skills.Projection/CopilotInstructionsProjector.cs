using System.Text;

namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Renders a canonical DNA skill as a path-scoped Copilot instruction file at
/// <c>.github/instructions/&lt;name&gt;.instructions.md</c>. Distinct from
/// <see cref="CopilotSkillProjector"/> (which mirrors the skill into Copilot's skill picker at
/// <c>.github/copilot/skills/&lt;name&gt;/SKILL.md</c>) — this is the lossy-edge surface that
/// participates in Copilot's instruction system, scoped via <c>applyTo:</c> glob.
/// </summary>
/// <remarks>
/// <para>
/// Phase-1 lossy-edge renderer per SUC-07. The <c>.instructions.md</c> shape is what Copilot in
/// VS Code and the GitHub PR-suggestion surface load alongside <c>.github/copilot-instructions.md</c>.
/// Behaviour per posture:
/// </para>
/// <list type="bullet">
///   <item><c>posture: always-on</c> → <c>applyTo: "**"</c> (matches everything in the repo).</item>
///   <item><c>posture: path-scoped</c> with <c>paths:</c> sequence → <c>applyTo: "&lt;glob&gt;"</c>;
///     multiple paths flatten into a single comma-separated <c>applyTo</c> string per
///     Copilot's documented format (e.g. <c>applyTo: "**/*.ts,**/*.tsx"</c>).</item>
///   <item>Other posture (invokable, manual, missing) → file is still emitted but with
///     <c>applyTo: ""</c> and a warning indicating it will not auto-load.</item>
/// </list>
/// </remarks>
public sealed class CopilotInstructionsProjector : ISkillProjector
{
    /// <inheritdoc />
    public string ClientKind => "copilot-instructions";

    /// <inheritdoc />
    public SkillProjection Project(SkillManifest manifest, ProjectionContext context)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(context);

        var warnings = new List<SkillProjectionWarning>();
        var posture = FrontmatterFieldReader.Read(manifest.FrontmatterRaw, "posture");

        string applyTo;
        if (string.Equals(posture, "always-on", StringComparison.Ordinal))
        {
            applyTo = "**";
        }
        else if (string.Equals(posture, "path-scoped", StringComparison.Ordinal))
        {
            var paths = FrontmatterFieldReader.ReadSequence(manifest.FrontmatterRaw, "paths");
            if (paths.Count == 0)
            {
                applyTo = string.Empty;
                warnings.Add(new SkillProjectionWarning(
                    Code: "copilot_instructions_missing_paths",
                    Message: $"Skill '{manifest.Name}' declares posture=path-scoped but has no 'paths:' sequence; " +
                             "Copilot instruction emitted with empty applyTo (will not auto-load)."));
            }
            else
            {
                applyTo = string.Join(",", paths);
            }
        }
        else
        {
            applyTo = string.Empty;
            if (!string.IsNullOrEmpty(posture))
            {
                warnings.Add(new SkillProjectionWarning(
                    Code: "copilot_instructions_non_auto_posture",
                    Message: $"Skill '{manifest.Name}' has posture='{posture}'; Copilot instruction emitted with empty applyTo."));
            }
        }

        var description = FrontmatterFieldReader.Read(manifest.FrontmatterRaw, "description");
        var body = SkillMarkdownLinkRewriter.RewriteFromCanonical(
            manifest.Body.TrimEnd('\n'),
            targetDepthFromRepoRoot: 2);
        var sb = new StringBuilder();
        sb.Append("---\n");
        sb.Append("applyTo: \"").Append(applyTo).Append("\"\n");
        sb.Append("description: ").Append(description).Append('\n');
        sb.Append("---\n");
        sb.Append(body).Append('\n');

        return new SkillProjection(
            TargetPath: ".github/instructions",
            FileName: $"{manifest.Name}.instructions.md",
            Contents: sb.ToString(),
            Mode: SkillProjectionMode.Write,
            Warnings: warnings);
    }
}
