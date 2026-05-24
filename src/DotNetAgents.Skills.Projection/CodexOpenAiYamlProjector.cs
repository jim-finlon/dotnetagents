// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Emits the Codex <c>agents/openai.yaml</c> sidecar that travels next to a canonical
/// SKILL.md when the skill explicitly opts out of model-driven invocation
/// (<c>invocation.modelInvokable: false</c>). The sidecar carries
/// <c>allow_implicit_invocation: false</c> so Codex respects the skill author's intent and only
/// invokes the skill on direct user or operator request.
/// </summary>
/// <remarks>
/// <para>
/// SUC-05 follow-up (<c>e4862196</c>). The trigger is the nested invocation block in the
/// canonical frontmatter:
/// </para>
/// <code language="yaml">
/// invocation:
///   modelInvokable: false
/// </code>
/// <para>
/// When the trigger is missing or <c>modelInvokable</c> is anything other than <c>false</c>,
/// <see cref="AppliesTo"/> returns false and the regen orchestrator skips emitting the sidecar.
/// The Codex SKILL.md mirror itself is unaffected — that still ships from
/// <see cref="CodexSkillProjector"/>.
/// </para>
/// </remarks>
public sealed class CodexOpenAiYamlProjector : ISkillProjector
{
    /// <inheritdoc />
    public string ClientKind => "codex-openai-yaml";

    /// <summary>
    /// Returns <c>true</c> when the canonical frontmatter declares
    /// <c>invocation.modelInvokable: false</c>. Callers (the regen orchestrator) must invoke
    /// this before <see cref="Project"/> and skip when it returns false — otherwise the
    /// projector throws to prevent an accidental no-op write.
    /// </summary>
    public bool AppliesTo(SkillManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var value = FrontmatterFieldReader.ReadNested(manifest.FrontmatterRaw, "invocation", "modelInvokable");
        return string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public SkillProjection Project(SkillManifest manifest, ProjectionContext context)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(context);

        if (!AppliesTo(manifest))
        {
            throw new InvalidOperationException(
                $"CodexOpenAiYamlProjector.Project called for skill '{manifest.Name}' which is not " +
                "invocation.modelInvokable=false. Callers must check AppliesTo first.");
        }

        // Sidecar lands next to the Codex SKILL.md at .agents/skills/<name>/agents/openai.yaml.
        // Keeping it under the skill directory keeps the projection set self-contained per skill.
        var targetPath = $".agents/skills/{manifest.Name}/agents";
        var contents = RenderYaml(manifest.Name);

        return new SkillProjection(
            TargetPath: targetPath,
            FileName: "openai.yaml",
            Contents: contents,
            Mode: SkillProjectionMode.Sidecar,
            Warnings: Array.Empty<SkillProjectionWarning>());
    }

    private static string RenderYaml(string skillName)
    {
        // Minimal sidecar matching Codex's documented agent-skills contract for opting out of
        // model-driven invocation. Stable formatting + trailing newline so re-projection is
        // byte-deterministic.
        return $"# SPDX-License-Identifier: Apache-2.0\n" +
               $"# Codex agents/openai.yaml sidecar for skill '{skillName}'.\n" +
               $"# Emitted by CodexOpenAiYamlProjector when canonical invocation.modelInvokable=false.\n" +
               $"allow_implicit_invocation: false\n";
    }
}
