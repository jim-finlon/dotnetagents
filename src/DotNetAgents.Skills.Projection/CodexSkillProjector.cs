// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Projects a canonical DNA skill to Codex's expected layout at
/// <c>.agents/skills/&lt;name&gt;/SKILL.md</c> (per the Codex agent-skills doc lookup order).
/// </summary>
/// <remarks>
/// <para>
/// Phase-1 dedicated projector per review decision record <c>59f04e03</c> (Option C). Codex
/// is an agentskills.io signatory, so the projected file content is byte-identical to the
/// canonical source when no Claude-Code superset fields are present. Claude superset fields
/// are stripped with a per-field Codex-tagged warning — same posture as the other dedicated
/// file-router projectors.
/// </para>
/// <para>
/// The optional <c>agents/openai.yaml</c> sidecar (emitted when a skill sets
/// <c>invocation.modelInvokable=false</c> so Codex respects <c>allow_implicit_invocation=false</c>)
/// is a follow-up — it depends on structured parsing of the canonical manifest's invocation
/// block which lands when the registry HTTP API (SUC-02) consumes the dna.skill.v1 schema. The
/// AGENTS.md graft for posture=always-on skills lives in <see cref="AgentsMdProjector"/>.
/// </para>
/// </remarks>
public sealed class CodexSkillProjector : ISkillProjector
{
    private static readonly string[] ClaudeSupersetFieldNames =
    [
        "allowed-tools",
        "model",
        "effort",
        "disable-model-invocation",
        "user-invocable",
        "argument-hint",
        "arguments",
        "agent",
        "hooks",
        "paths",
        "shell",
        "when_to_use",
        "context",
    ];

    /// <inheritdoc />
    public string ClientKind => "codex";

    /// <inheritdoc />
    public SkillProjection Project(SkillManifest manifest, ProjectionContext context)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(context);

        var warnings = new List<SkillProjectionWarning>();
        var frontmatter = manifest.FrontmatterRaw;

        if (context.StripSupersetFields)
        {
            foreach (var field in ClaudeSupersetFieldNames)
            {
                var stripped = FrontmatterFieldStripper.Strip(frontmatter, field);
                if (!ReferenceEquals(stripped, frontmatter))
                {
                    frontmatter = stripped;
                    warnings.Add(new SkillProjectionWarning(
                        Code: "stripped_superset_field",
                        Message: $"Removed Claude-Code superset field '{field}' from Codex projection " +
                                 "(Codex's agent-skills SKILL.md only consumes agentskills.io baseline)."));
                }
            }
        }

        var contents = $"---\n{frontmatter}\n---\n{manifest.Body}";
        var targetPath = $".agents/skills/{manifest.Name}";

        return new SkillProjection(
            TargetPath: targetPath,
            FileName: "SKILL.md",
            Contents: contents,
            Mode: SkillProjectionMode.Write,
            Warnings: warnings);
    }
}
