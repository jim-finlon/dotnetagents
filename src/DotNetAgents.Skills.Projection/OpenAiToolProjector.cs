using System.Text.Json;

namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Projects a canonical DNA skill to the SaaS-OpenAI surface set: Codex Cloud, the Assistants /
/// Responses API, and GPT Actions. Emits two artifacts per skill — the OpenAI function tool JSON
/// (consumed by the OpenAI Tools API and GPT-Action manifests) and a sibling
/// <c>.system-prefix.md</c> carrying the SKILL.md body so Assistants/Responses callers can paste
/// it directly into <c>instructions</c>.
/// </summary>
/// <remarks>
/// <para>
/// Phase-1 lossy-edge renderer per SUC-07. Distinct from
/// <see cref="LocalLlmToolProjector"/>, which targets <c>.local-llm/skills/</c> for vLLM-hosted
/// local models. The SaaS surface lands at <c>.openai/skills/&lt;name&gt;/</c>:
/// <list type="bullet">
///   <item><c>tool.json</c> — OpenAI function tool shape, model-agnostic.</item>
///   <item><c>.system-prefix.md</c> — Markdown prefix for the <c>instructions</c> / <c>system</c> field.</item>
/// </list>
/// </para>
/// <para>
/// Because <see cref="ISkillProjector.Project"/> returns a single
/// <see cref="SkillProjection"/>, the projector is parameterised at construction time by an
/// <see cref="OpenAiToolProjectionKind"/> — same pattern as SUC-06's local-LLM projector. The
/// regen orchestrator instantiates one of each kind per canonical skill.
/// </para>
/// </remarks>
public sealed class OpenAiToolProjector : ISkillProjector
{
    private readonly OpenAiToolProjectionKind _kind;

    public OpenAiToolProjector(OpenAiToolProjectionKind kind)
    {
        _kind = kind;
    }

    /// <inheritdoc />
    public string ClientKind => _kind switch
    {
        OpenAiToolProjectionKind.ToolJson => "openai-tool-json",
        OpenAiToolProjectionKind.SystemPrefix => "openai-system-prefix",
        _ => throw new InvalidOperationException($"Unknown OpenAiToolProjectionKind: {_kind}"),
    };

    /// <inheritdoc />
    public SkillProjection Project(SkillManifest manifest, ProjectionContext context)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(context);

        var targetPath = $".openai/skills/{manifest.Name}";
        return _kind switch
        {
            OpenAiToolProjectionKind.ToolJson => new SkillProjection(
                TargetPath: targetPath,
                FileName: "tool.json",
                Contents: RenderToolJson(manifest),
                Mode: SkillProjectionMode.Write,
                Warnings: Array.Empty<SkillProjectionWarning>()),
            OpenAiToolProjectionKind.SystemPrefix => new SkillProjection(
                TargetPath: targetPath,
                FileName: ".system-prefix.md",
                Contents: RenderSystemPrefix(manifest),
                Mode: SkillProjectionMode.Sidecar,
                Warnings: Array.Empty<SkillProjectionWarning>()),
            _ => throw new InvalidOperationException($"Unknown OpenAiToolProjectionKind: {_kind}"),
        };
    }

    private static string RenderToolJson(SkillManifest manifest)
    {
        var description = FrontmatterFieldReader.Read(manifest.FrontmatterRaw, "description");
        // Phase-1 emits an empty parameters object — structured parameter schemas land with the
        // registry HTTP API (SUC-02) consuming dna.skill.v1. The empty shape is a valid GPT-Action
        // / Assistants tool schema today.
        var doc = new
        {
            type = "function",
            function = new
            {
                name = manifest.Name,
                description,
                parameters = new
                {
                    type = "object",
                    properties = new { },
                    required = Array.Empty<string>(),
                },
            },
        };
        return JsonSerializer.Serialize(doc, JsonOpts) + "\n";
    }

    private static string RenderSystemPrefix(SkillManifest manifest)
    {
        var description = FrontmatterFieldReader.Read(manifest.FrontmatterRaw, "description");
        // The system-prefix wraps the SKILL.md body so an Assistants/Responses caller can paste
        // it directly into the `instructions` (Assistants) or top-level `system` (Responses) field.
        var body = manifest.Body.TrimEnd('\n');
        return $"<!-- DNA skill: {manifest.Name} -->\n{description}\n\n{body}\n";
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}

/// <summary>Which SaaS-OpenAI artifact an <see cref="OpenAiToolProjector"/> emits.</summary>
public enum OpenAiToolProjectionKind
{
    /// <summary>OpenAI function tool JSON written to <c>tool.json</c>.</summary>
    ToolJson,

    /// <summary>System-prefix Markdown for Assistants/Responses <c>instructions</c>, written to <c>.system-prefix.md</c>.</summary>
    SystemPrefix,
}
