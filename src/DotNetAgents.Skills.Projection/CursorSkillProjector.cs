// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Projects a canonical DNA skill to Cursor's expected layout
/// (<c>.cursor/skills/&lt;name&gt;/SKILL.md</c>).
/// </summary>
/// <remarks>
/// <para>
/// Cursor signed the Dec 2025 agentskills.io open standard, so SKILL.md content is byte-for-byte
/// identical between the canonical <c>dna-skills/&lt;domain&gt;/&lt;name&gt;/SKILL.md</c> source and
/// the projected <c>.cursor/skills/&lt;name&gt;/SKILL.md</c> output, with one exception: Claude-Code
/// superset fields (<c>allowed-tools</c>, <c>model</c>, <c>effort</c>, <c>disable-model-invocation</c>,
/// <c>user-invocable</c>, <c>argument-hint</c>, <c>arguments</c>, <c>agent</c>, <c>hooks</c>,
/// <c>paths</c>, <c>shell</c>) are stripped when present, since Cursor does not understand them and
/// the spec recommends emitting a warning. See <c>docs/requirements/skills-universal-catalog/VENDOR-PROJECTION-CONTRACTS.md</c>.
/// </para>
/// <para>
/// Anchor decision: operator intake form <c>88404ff6</c> picked <c>repo</c> scope by default
/// (skills ship with the workspace, pass code review).
/// </para>
/// </remarks>
public sealed class CursorSkillProjector : ISkillProjector
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
    public string ClientKind => "cursor";

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
                        Message: $"Removed Claude-Code superset field '{field}' from Cursor projection (Cursor ignores it)."));
                }
            }
        }

        var contents = $"---\n{frontmatter}\n---\n{manifest.Body}";

        // Cursor uses flat <name> layout (the agentskills.io convention) regardless of canonical
        // domain bucketing. Canonical = skills/local-infrastructure/service-index/SKILL.md becomes
        // .cursor/skills/tyr-index/SKILL.md.
        var targetPath = $".cursor/skills/{manifest.Name}";

        return new SkillProjection(
            TargetPath: targetPath,
            FileName: "SKILL.md",
            Contents: contents,
            Mode: SkillProjectionMode.Write,
            Warnings: warnings);
    }

    /// <summary>
    /// Remove a single top-level YAML key (and its block-scalar continuation lines, if any) from a
    /// frontmatter block. Returns the original instance when the field is absent so the caller can
    /// detect a no-op via reference equality.
    /// </summary>
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
        while (trimmedStart < line.Length && line[trimmedStart] == ' ')
        {
            trimmedStart++;
        }
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
