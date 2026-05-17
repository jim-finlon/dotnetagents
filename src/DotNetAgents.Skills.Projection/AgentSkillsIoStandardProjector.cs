using System.Text.RegularExpressions;

namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Config-driven projector covering the agentskills.io signatories that share the open-standard
/// SKILL.md shape exactly (no Claude Code superset, no Cursor rule shape, no Codex AGENTS.md
/// grafting). One <see cref="AgentSkillsIoStandardProjector"/> instance is created per entry in
/// <c>config/skills-projection/agentskills-io-targets.json</c>; each instance projects to a single
/// signatory's expected path (e.g. <c>.goose/skills/&lt;name&gt;/SKILL.md</c>).
/// </summary>
/// <remarks>
/// <para>
/// Review decision record <c>59f04e03</c> (2026-05-14, Option C) picked this design: maintain a
/// small set of dedicated projectors for surfaces with real divergence (Claude Code superset,
/// Cursor rules vs skills, Codex AGENTS.md + <c>agents/openai.yaml</c> sidecar, local-LLM tool-call
/// JSON, OpenAI function JSON) and use a single config-driven projector for everything else.
/// Adding a new agentskills.io signatory takes one JSON entry — zero code change.
/// </para>
/// <para>
/// Behavior matches <see cref="CursorSkillProjector"/>: agentskills.io baseline frontmatter is
/// preserved verbatim; Claude Code-superset fields are stripped (with a warning each) so the
/// projected output is a clean agentskills.io baseline SKILL.md.
/// </para>
/// </remarks>
public sealed class AgentSkillsIoStandardProjector : ISkillProjector
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

    private readonly string _clientKind;
    private readonly string _relativePath;

    /// <summary>Create a projector bound to a single signatory + target path.</summary>
    /// <param name="target">Path-map entry loaded from <see cref="AgentSkillsIoTargetMapLoader"/>.</param>
    public AgentSkillsIoStandardProjector(AgentSkillsIoTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(target.Client);
        ArgumentException.ThrowIfNullOrWhiteSpace(target.RelativePath);
        _clientKind = target.Client;
        // Normalize the relative path: trim trailing separators so the per-skill suffix joins cleanly.
        _relativePath = target.RelativePath.TrimEnd('/', '\\');
    }

    /// <inheritdoc />
    public string ClientKind => _clientKind;

    /// <summary>
    /// Convenience factory that loads <paramref name="targetMapJsonPath"/> and returns one
    /// projector per entry.
    /// </summary>
    public static IReadOnlyList<AgentSkillsIoStandardProjector> CreateAllFromConfig(string targetMapJsonPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetMapJsonPath);
        var map = AgentSkillsIoTargetMapLoader.Load(targetMapJsonPath);
        return map.Targets.Select(t => new AgentSkillsIoStandardProjector(t)).ToList();
    }

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
                var stripped = StripFrontmatterField(frontmatter, field);
                if (!ReferenceEquals(stripped, frontmatter))
                {
                    frontmatter = stripped;
                    warnings.Add(new SkillProjectionWarning(
                        Code: "stripped_superset_field",
                        Message: $"Removed Claude-Code superset field '{field}' from {_clientKind} projection."));
                }
            }
        }

        var contents = $"---\n{frontmatter}\n---\n{manifest.Body}";
        var targetPath = $"{_relativePath}/{manifest.Name}";

        return new SkillProjection(
            TargetPath: targetPath,
            FileName: "SKILL.md",
            Contents: contents,
            Mode: SkillProjectionMode.Write,
            Warnings: warnings);
    }

    private static string StripFrontmatterField(string frontmatter, string fieldName)
    {
        var lines = frontmatter.Split('\n');
        var output = new List<string>(lines.Length);
        var i = 0;
        var stripped = false;
        while (i < lines.Length)
        {
            var line = lines[i];
            if (IsTopLevelKeyMatch(line, fieldName))
            {
                stripped = true;
                i++;
                while (i < lines.Length && IsBlockScalarContinuation(lines[i]))
                {
                    i++;
                }
                continue;
            }
            output.Add(line);
            i++;
        }
        return stripped ? string.Join('\n', output) : frontmatter;
    }

    private static bool IsTopLevelKeyMatch(string line, string fieldName)
    {
        var trimmedStart = 0;
        while (trimmedStart < line.Length && line[trimmedStart] == ' ') trimmedStart++;
        if (trimmedStart != 0) return false;
        if (line.Length <= fieldName.Length) return false;
        if (!line.AsSpan(0, fieldName.Length).SequenceEqual(fieldName.AsSpan())) return false;
        return line[fieldName.Length] == ':';
    }

    private static bool IsBlockScalarContinuation(string line)
    {
        if (line.Length == 0) return false;
        return line[0] == ' ' || line[0] == '\t' || line[0] == '-';
    }
}
