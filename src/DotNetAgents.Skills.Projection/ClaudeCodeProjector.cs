using System.Text.RegularExpressions;

namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Projects a canonical DNA skill to Claude Code's expected layout.
/// </summary>
/// <remarks>
/// <para>
/// Review decision record <c>88404ff6</c> (2026-05-14) locked <strong>repo-scope</strong> as the
/// default: emit to <c>.claude/skills/&lt;name&gt;/SKILL.md</c> so skills ship with the workspace and
/// pass code review. User-scope (<c>~/.claude/skills/&lt;name&gt;/</c>) is opt-in via
/// <see cref="ProjectionContext.TargetScope"/>=<see cref="ProjectionTargetScope.User"/> (which the
/// generic manifest exposes as <c>projectionHints.claude.targetScope: user</c>).
/// </para>
/// <para>
/// Claude Code's frontmatter is a <em>superset</em> of the agentskills.io baseline. Fields like
/// <c>allowed-tools</c>, <c>model</c>, <c>effort</c>, <c>disable-model-invocation</c>,
/// <c>user-invocable</c>, <c>argument-hint</c>, <c>arguments</c>, <c>agent</c>, <c>hooks</c>,
/// <c>paths</c>, <c>shell</c>, <c>when_to_use</c>, <c>context</c> are <em>preserved</em> here (in
/// contrast to <see cref="CursorSkillProjector"/> which strips them with a warning).
/// </para>
/// <para>
/// Anthropic's Claude Code Skills policy forbids skill names containing <c>anthropic</c> or
/// <c>claude</c>. This projector refuses such names by throwing
/// <see cref="InvalidOperationException"/>; the schema validator at
/// <c>docs/schemas/dna.skill.v1.schema.json</c> + <c>scripts/Test-DnaAgentSkills.ps1</c> already
/// rejects them at authoring time, so reaching this guard means the canonical source slipped past
/// the validator.
/// </para>
/// </remarks>
public sealed class ClaudeCodeProjector : ISkillProjector
{
    private static readonly Regex AnthropicClaudeNameRe = new(
        @"(anthropic|claude)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <inheritdoc />
    public string ClientKind => "claude-code";

    /// <inheritdoc />
    public SkillProjection Project(SkillManifest manifest, ProjectionContext context)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(context);

        if (AnthropicClaudeNameRe.IsMatch(manifest.Name))
        {
            throw new InvalidOperationException(
                $"Skill name '{manifest.Name}' is forbidden by Anthropic Claude Code Skills policy " +
                "(must not contain 'anthropic' or 'claude'). Rename the canonical skill in dna-skills/ " +
                "and re-run scripts/Test-DnaAgentSkills.ps1.");
        }

        // Repo scope (intake 88404ff6 default): .claude/skills/<name>
        // User scope (opt-in): ~/.claude/skills/<name> — projector emits the path with a leading "~"
        // so the orchestrator that writes the file resolves the actor home as needed.
        var targetPath = context.TargetScope switch
        {
            ProjectionTargetScope.User => $"~/.claude/skills/{manifest.Name}",
            _ => $".claude/skills/{manifest.Name}",
        };

        // Claude Code consumes the same agentskills.io baseline; superset fields are preserved.
        // No frontmatter manipulation needed for the byte-identical case (canonical = Claude).
        var contents = $"---\n{manifest.FrontmatterRaw}\n---\n{manifest.Body}";

        return new SkillProjection(
            TargetPath: targetPath,
            FileName: "SKILL.md",
            Contents: contents,
            Mode: SkillProjectionMode.Write,
            Warnings: Array.Empty<SkillProjectionWarning>());
    }
}
