// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;

namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Grafts <c>posture: always-on</c> skills into the repo-root <c>AGENTS.md</c> with idempotent
/// comment markers so re-projection replaces the same section in place rather than appending.
/// </summary>
/// <remarks>
/// <para>
/// Codex reads <c>AGENTS.md</c> root-to-leaf at session start and prepends each file as a user-role
/// message. <c>posture: always-on</c> skills want exactly that load order, so this projector emits
/// a marker-bounded section per skill. Phase-1 scope is just the repo-root <c>AGENTS.md</c>; the
/// <c>.github/copilot-instructions.md</c> graft for Copilot path-scoped skills is the equivalent
/// SUC-07 deliverable.
/// </para>
/// <para>
/// Marker format: <c>&lt;!-- dna-skill:&lt;name&gt;:begin --&gt;</c> through
/// <c>&lt;!-- dna-skill:&lt;name&gt;:end --&gt;</c>. Skills whose frontmatter does not declare
/// <c>posture: always-on</c> are skipped via <see cref="AppliesTo"/>; the regen orchestrator filters.
/// </para>
/// </remarks>
public sealed class AgentsMdProjector : ISkillProjector
{
    private static readonly Regex PostureRe = new(
        @"^\s*posture:\s*(?<v>[^\s#]+)\s*$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    /// <inheritdoc />
    public string ClientKind => "agents-md";

    /// <summary>
    /// Returns <c>true</c> when the skill's frontmatter declares <c>posture: always-on</c>.
    /// Callers (the regen orchestrator) must invoke this before <see cref="Project"/> and skip
    /// the projector for non-applicable skills.
    /// </summary>
    public bool AppliesTo(SkillManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var match = PostureRe.Match(manifest.FrontmatterRaw);
        return match.Success && string.Equals(match.Groups["v"].Value, "always-on", StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public SkillProjection Project(SkillManifest manifest, ProjectionContext context)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(context);

        if (!AppliesTo(manifest))
        {
            throw new InvalidOperationException(
                $"AgentsMdProjector.Project called for skill '{manifest.Name}' which is not posture=always-on. " +
                "Callers must check AppliesTo first.");
        }

        // Each AppendSection projection contributes one marker-bounded section. The orchestrator
        // is responsible for reading AGENTS.md, replacing the existing section between the
        // matching markers, or appending a new section to the end of the file.
        var sectionContent = RenderSection(manifest);

        return new SkillProjection(
            TargetPath: ".",            // repo root
            FileName: "AGENTS.md",
            Contents: sectionContent,
            Mode: SkillProjectionMode.AppendSection,
            Warnings: Array.Empty<SkillProjectionWarning>(),
            // Story e399bfce: identify the section to the applier so it can drive the
            // marker-bounded graft without re-deriving the key.
            MarkerKey: manifest.Name);
    }

    /// <summary>
    /// Apply (or replace) a marker-bounded section in the supplied AGENTS.md content with
    /// <paramref name="newSection"/>. Idempotent: running twice with the same input yields the
    /// same output. Used by the regen orchestrator to graft AppendSection projections.
    /// </summary>
    /// <param name="existingFileContent">Current AGENTS.md content (or empty string when the file does not exist).</param>
    /// <param name="skillName">Skill name from <c>SkillManifest.Name</c>; used to compose the marker pair.</param>
    /// <param name="newSection">The section body to write between the markers (does NOT include the markers themselves).</param>
    /// <returns>The full AGENTS.md content with the section grafted in place (or appended) and a trailing newline.</returns>
    public static string ApplyGraft(string existingFileContent, string skillName, string newSection)
        => MarkerBoundedSectionWriter.ApplySection(existingFileContent, skillName, newSection);

    private static string RenderSection(SkillManifest manifest)
    {
        // The section format is the skill body — Codex already prepends a "# AGENTS.md
        // instructions for <dir>" header, so we don't double-up. We do add a one-line heading
        // with the skill name so operators reading AGENTS.md can identify the source skill.
        return $"## {manifest.Name}\n\n{manifest.Body.TrimEnd('\n')}";
    }
}
