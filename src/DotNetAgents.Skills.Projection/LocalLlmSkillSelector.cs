// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;

namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Given a model's tool-call capability and a per-skill projection directory under
/// <c>.local-llm/skills/&lt;skill-name&gt;/</c>, pick the right pre-projected artifact set for the
/// orchestrator to inject at session start.
/// </summary>
/// <remarks>
/// <para>Story 758e4e2d follow-up to SUC-06 (LocalLlmToolProjector). Selection rules:</para>
/// <list type="bullet">
///   <item>
///     <description>
///     If <c>toolFunctionSupport</c> is <c>true</c> AND the supplied template-family token parses
///     to a known <see cref="LocalLlmTemplateFamily"/> AND both <c>tool.json</c> and
///     <c>template-hint.json</c> are present on disk → emit a
///     <see cref="LocalLlmSkillSelection.ToolCall"/>.
///     </description>
///   </item>
///   <item>
///     <description>
///     Otherwise (no tool support, unknown family token, or missing tool/template files) → fall
///     back to <see cref="LocalLlmSkillSelection.PlainPrompt"/> reading <c>prompt-fragment.md</c>.
///     </description>
///   </item>
/// </list>
/// <para>
/// Phase-1 fallback per the story constraints: the selector reads the local file tree directly so
/// it works even before the skills registry HTTP API (SUC-02) ships. When SUC-02 lands, a
/// future overload can resolve from the registry without breaking this contract.
/// </para>
/// <para>
/// Selection results are cached per <c>(skillDirectoryAbsolutePath, templateFamily, mode)</c>
/// to keep cost O(1) at session start. The cache is content-immutable: artifact bodies are
/// re-read only when the cache is invalidated (caller-side decision).
/// </para>
/// </remarks>
public sealed class LocalLlmSkillSelector
{
    private readonly ConcurrentDictionary<string, LocalLlmSkillSelection> _cache = new(StringComparer.Ordinal);

    /// <summary>
    /// Select the local-LLM artifact set for one skill given the model's tool-call posture.
    /// </summary>
    /// <param name="toolFunctionSupport">
    /// From <c>LlmGatewayCatalogEntry.ToolFunctionSupport</c>. When false, the selector always
    /// returns <see cref="LocalLlmSkillSelection.PlainPrompt"/>.
    /// </param>
    /// <param name="templateFamilyToken">
    /// From <c>LlmGatewayCatalogEntry.TemplateFamily</c>. Parsed via
    /// <see cref="LocalLlmToolProjector.TryParseTemplateFamily"/>. Null/empty/unknown falls back
    /// to plain-prompt even when <paramref name="toolFunctionSupport"/> is true.
    /// </param>
    /// <param name="skillDirectoryAbsolutePath">
    /// Absolute path to <c>.local-llm/skills/&lt;skill-name&gt;/</c> containing the three
    /// projected files. The selector does NOT crawl above this directory.
    /// </param>
    /// <returns>The chosen artifact set with body strings read from disk.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the skill directory is missing or the plain-prompt fallback file
    /// (<c>prompt-fragment.md</c>) is absent — a skill without the plain-prompt fragment cannot be
    /// invoked safely on a local LLM.
    /// </exception>
    public LocalLlmSkillSelection Select(
        bool toolFunctionSupport,
        string? templateFamilyToken,
        string skillDirectoryAbsolutePath)
    {
        if (string.IsNullOrWhiteSpace(skillDirectoryAbsolutePath))
        {
            throw new ArgumentException("Skill directory path cannot be empty.", nameof(skillDirectoryAbsolutePath));
        }
        if (!Directory.Exists(skillDirectoryAbsolutePath))
        {
            throw new ArgumentException(
                $"Skill directory does not exist: {skillDirectoryAbsolutePath}",
                nameof(skillDirectoryAbsolutePath));
        }

        var family = LocalLlmToolProjector.TryParseTemplateFamily(templateFamilyToken);
        var mode = toolFunctionSupport && family.HasValue
            ? SelectionMode.ToolCall
            : SelectionMode.PlainPrompt;
        var cacheKey = $"{skillDirectoryAbsolutePath}{templateFamilyToken ?? string.Empty}{mode}";

        return _cache.GetOrAdd(cacheKey, _ => Load(skillDirectoryAbsolutePath, family, mode));
    }

    /// <summary>Drop all cached selections. Useful when the orchestrator regenerates artifacts.</summary>
    public void InvalidateCache() => _cache.Clear();

    private static LocalLlmSkillSelection Load(
        string skillDir,
        LocalLlmTemplateFamily? family,
        SelectionMode mode)
    {
        if (mode == SelectionMode.ToolCall && family is { } parsedFamily)
        {
            var toolJsonPath = Path.Combine(skillDir, "tool.json");
            var templateHintPath = Path.Combine(skillDir, "template-hint.json");
            if (File.Exists(toolJsonPath) && File.Exists(templateHintPath))
            {
                return new LocalLlmSkillSelection.ToolCall(
                    SkillDirectory: skillDir,
                    TemplateFamily: parsedFamily,
                    ToolJson: File.ReadAllText(toolJsonPath),
                    TemplateHintJson: File.ReadAllText(templateHintPath));
            }
            // Tool/template files missing — fall through to plain-prompt fallback. Per the story's
            // acceptance the "missing template hint / family mismatch" path falls back instead of
            // throwing, so the orchestrator can still invoke the skill via plain prompt.
        }

        var promptFragmentPath = Path.Combine(skillDir, "prompt-fragment.md");
        if (!File.Exists(promptFragmentPath))
        {
            throw new ArgumentException(
                $"Skill directory is missing prompt-fragment.md (no fallback available): {skillDir}",
                nameof(skillDir));
        }
        return new LocalLlmSkillSelection.PlainPrompt(
            SkillDirectory: skillDir,
            PromptFragmentMarkdown: File.ReadAllText(promptFragmentPath));
    }

    private enum SelectionMode
    {
        ToolCall,
        PlainPrompt,
    }
}

/// <summary>
/// Discriminated union of the two artifact sets the orchestrator can inject for a skill.
/// </summary>
public abstract record LocalLlmSkillSelection(string SkillDirectory)
{
    /// <summary>The model supports tool-calling and the gateway reported a known template family.</summary>
    public sealed record ToolCall(
        string SkillDirectory,
        LocalLlmTemplateFamily TemplateFamily,
        string ToolJson,
        string TemplateHintJson)
        : LocalLlmSkillSelection(SkillDirectory);

    /// <summary>The model lacks tool-calling support or the family is unknown/missing.</summary>
    public sealed record PlainPrompt(
        string SkillDirectory,
        string PromptFragmentMarkdown)
        : LocalLlmSkillSelection(SkillDirectory);
}
