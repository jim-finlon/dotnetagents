using System.Text.Json;

namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Projects a canonical DNA skill to the three artifacts a privately-hosted local LLM needs to
/// invoke the skill: an OpenAI-compatible tool function JSON, a vLLM tool-parser template hint,
/// and a plain-prompt fragment for models that lack tool-calling.
/// </summary>
/// <remarks>
/// <para>
/// Phase-1 dedicated projector per review decision record <c>59f04e03</c> (Option C). Because
/// <see cref="ISkillProjector.Project"/> returns a single <see cref="SkillProjection"/>, this
/// projector is parameterized at construction time by a <see cref="LocalLlmProjectionKind"/>: a
/// caller wanting all three outputs instantiates the projector three times (the regen orchestrator
/// does this once with the per-skill template family resolved from the catalog).
/// </para>
/// <para>
/// Target layout under repo root:
/// <list type="bullet">
///   <item><c>.local-llm/skills/&lt;name&gt;/tool.json</c> — OpenAI function shape</item>
///   <item><c>.local-llm/skills/&lt;name&gt;/template-hint.json</c> — vLLM <c>--tool-call-parser</c> hint</item>
///   <item><c>.local-llm/skills/&lt;name&gt;/prompt-fragment.md</c> — plain-prompt fallback</item>
/// </list>
/// </para>
/// <para>
/// The gateway+orchestrator wiring (<c>LlmGatewayCatalog.ToolFunctionSupport</c> flag → projector
/// selection at session start) and the live vLLM Qwen 2.5 smoke test are tracked as follow-ups so
/// SUC-06 can land the deterministic projector first.
/// </para>
/// </remarks>
public sealed class LocalLlmToolProjector : ISkillProjector
{
    private readonly LocalLlmProjectionKind _kind;
    private readonly LocalLlmTemplateFamily _templateFamily;

    /// <summary>
    /// Create a projector that emits one of the three local-LLM artifacts.
    /// </summary>
    /// <param name="kind">Which artifact to emit.</param>
    /// <param name="templateFamily">
    /// vLLM tool-parser family. Only consulted when <paramref name="kind"/> is
    /// <see cref="LocalLlmProjectionKind.VllmTemplateHint"/>; ignored otherwise.
    /// </param>
    public LocalLlmToolProjector(
        LocalLlmProjectionKind kind,
        LocalLlmTemplateFamily templateFamily = LocalLlmTemplateFamily.Hermes)
    {
        _kind = kind;
        _templateFamily = templateFamily;
    }

    /// <inheritdoc />
    public string ClientKind => _kind switch
    {
        LocalLlmProjectionKind.OpenAiToolJson => "local-llm-openai-tool",
        LocalLlmProjectionKind.VllmTemplateHint => $"local-llm-vllm-{TemplateFamilyToken(_templateFamily)}",
        LocalLlmProjectionKind.PlainPromptFragment => "local-llm-plain-prompt",
        _ => throw new InvalidOperationException($"Unknown LocalLlmProjectionKind: {_kind}"),
    };

    /// <inheritdoc />
    public SkillProjection Project(SkillManifest manifest, ProjectionContext context)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(context);

        var targetPath = $".local-llm/skills/{manifest.Name}";
        return _kind switch
        {
            LocalLlmProjectionKind.OpenAiToolJson => new SkillProjection(
                TargetPath: targetPath,
                FileName: "tool.json",
                Contents: RenderOpenAiToolJson(manifest),
                Mode: SkillProjectionMode.Write,
                Warnings: Array.Empty<SkillProjectionWarning>()),
            LocalLlmProjectionKind.VllmTemplateHint => new SkillProjection(
                TargetPath: targetPath,
                FileName: "template-hint.json",
                Contents: RenderVllmTemplateHint(manifest, _templateFamily),
                Mode: SkillProjectionMode.Sidecar,
                Warnings: Array.Empty<SkillProjectionWarning>()),
            LocalLlmProjectionKind.PlainPromptFragment => new SkillProjection(
                TargetPath: targetPath,
                FileName: "prompt-fragment.md",
                Contents: RenderPlainPromptFragment(manifest),
                Mode: SkillProjectionMode.Sidecar,
                Warnings: Array.Empty<SkillProjectionWarning>()),
            _ => throw new InvalidOperationException($"Unknown LocalLlmProjectionKind: {_kind}"),
        };
    }

    private static string RenderOpenAiToolJson(SkillManifest manifest)
    {
        var description = ExtractDescription(manifest.FrontmatterRaw);
        // Phase-1 emits an empty parameter object — structured parameter schemas land with the
        // registry HTTP API (SUC-02) consuming the dna.skill.v1 invocation block. The empty shape
        // is valid OpenAI tool JSON and is what local models receive today; structured params can
        // be grafted in without breaking consumers.
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

    private static string RenderVllmTemplateHint(SkillManifest manifest, LocalLlmTemplateFamily templateFamily)
    {
        var parserName = TemplateFamilyToken(templateFamily);
        var doc = new
        {
            schemaVersion = "dna.skills-projection.local-llm-template-hint.v1",
            skill = manifest.Name,
            templateFamily = parserName,
            vllmToolCallParser = parserName,
            vllmServeArgs = new[] { "--enable-auto-tool-choice", $"--tool-call-parser={parserName}" },
            notes = "Pass these args when starting vLLM so the local model can return tool calls " +
                    "in the format the DNA gateway expects (OpenAI-shape function call).",
        };
        return JsonSerializer.Serialize(doc, JsonOpts) + "\n";
    }

    private static string RenderPlainPromptFragment(SkillManifest manifest)
    {
        var description = ExtractDescription(manifest.FrontmatterRaw);
        // Plain-prompt fallback: wrap the SKILL.md body as a system-prompt fragment with explicit
        // invocation guidance. Used by the gateway when LlmGatewayCatalog.ToolFunctionSupport is
        // false (or unknown) for the target model.
        var body = manifest.Body.TrimEnd('\n');
        return $"# Skill: {manifest.Name}\n\n{description}\n\n## Instructions\n\n{body}\n\n## Invocation\n\nWhen the user request matches the trigger above, follow the instructions and return the result as your final response. Do not emit a tool-call envelope.\n";
    }

    private static string ExtractDescription(string frontmatter)
        => FrontmatterFieldReader.Read(frontmatter, "description");

    internal static string TemplateFamilyToken(LocalLlmTemplateFamily family) => family switch
    {
        LocalLlmTemplateFamily.Hermes => "hermes",
        LocalLlmTemplateFamily.Mistral => "mistral",
        LocalLlmTemplateFamily.Llama3Json => "llama3_json",
        LocalLlmTemplateFamily.DeepSeekV3 => "deepseek_v3",
        LocalLlmTemplateFamily.Xlam => "xlam",
        LocalLlmTemplateFamily.Pythonic => "pythonic",
        _ => throw new InvalidOperationException($"Unknown LocalLlmTemplateFamily: {family}"),
    };

    /// <summary>
    /// Parse the vLLM <c>--tool-call-parser</c> token the gateway reports back into a
    /// <see cref="LocalLlmTemplateFamily"/>. Returns null when the token is null/empty/unknown so
    /// the selector can fall back to plain-prompt without throwing.
    /// </summary>
    public static LocalLlmTemplateFamily? TryParseTemplateFamily(string? token) =>
        token?.Trim().ToLowerInvariant() switch
        {
            "hermes" => LocalLlmTemplateFamily.Hermes,
            "mistral" => LocalLlmTemplateFamily.Mistral,
            "llama3_json" => LocalLlmTemplateFamily.Llama3Json,
            "deepseek_v3" => LocalLlmTemplateFamily.DeepSeekV3,
            "xlam" => LocalLlmTemplateFamily.Xlam,
            "pythonic" => LocalLlmTemplateFamily.Pythonic,
            _ => null,
        };

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}

/// <summary>Which of the three local-LLM artifacts a <see cref="LocalLlmToolProjector"/> emits.</summary>
public enum LocalLlmProjectionKind
{
    /// <summary>OpenAI-shape function tool JSON written to <c>tool.json</c>.</summary>
    OpenAiToolJson,

    /// <summary>vLLM <c>--tool-call-parser</c> template hint written to <c>template-hint.json</c>.</summary>
    VllmTemplateHint,

    /// <summary>Plain-prompt fallback fragment written to <c>prompt-fragment.md</c>.</summary>
    PlainPromptFragment,
}

/// <summary>
/// vLLM tool-parser families recognised by the local-LLM gateway pipeline. Mirrors the
/// <c>--tool-call-parser</c> values that ship with vLLM (selected subset per SUC-06 acceptance
/// criteria: hermes | mistral | llama3_json | deepseek_v3 | xlam | pythonic).
/// </summary>
public enum LocalLlmTemplateFamily
{
    /// <summary>NousResearch Hermes-2-Pro family (<c>hermes</c>).</summary>
    Hermes,

    /// <summary>Mistral instruction-tuned models (<c>mistral</c>).</summary>
    Mistral,

    /// <summary>Llama 3 family using the JSON tool-call template (<c>llama3_json</c>).</summary>
    Llama3Json,

    /// <summary>DeepSeek V3 chat models (<c>deepseek_v3</c>).</summary>
    DeepSeekV3,

    /// <summary>Salesforce xLAM tool-calling models (<c>xlam</c>).</summary>
    Xlam,

    /// <summary>Pythonic tool-call syntax used by some open code models (<c>pythonic</c>).</summary>
    Pythonic,
}
