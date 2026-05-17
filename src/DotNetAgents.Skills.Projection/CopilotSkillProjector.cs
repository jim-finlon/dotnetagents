namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Projects a canonical DNA skill to GitHub Copilot's expected layout at
/// <c>.github/copilot/skills/&lt;name&gt;/SKILL.md</c> (matches the awesome-copilot convention).
/// </summary>
/// <remarks>
/// <para>
/// Phase-1 dedicated projector per review decision record <c>59f04e03</c> (Option C). Copilot is
/// an agentskills.io signatory. Relative Markdown link targets are rewritten for the deeper
/// <c>.github/copilot/skills/&lt;name&gt;/</c> directory; otherwise content matches the canonical
/// source when no Claude-Code superset fields are present. Claude superset fields are stripped
/// with a warning each — same posture as <see cref="CursorSkillProjector"/> and
/// <see cref="AgentSkillsIoStandardProjector"/>.
/// </para>
/// <para>
/// Posture variants (<c>posture: always-on</c> → <c>.github/copilot-instructions.md</c> graft and
/// <c>posture: path-scoped</c> → <c>.github/instructions/&lt;name&gt;.instructions.md</c> with
/// <c>applyTo</c> glob) are <strong>not</strong> shipped here — they're co-owned by SUC-07
/// (lossy-edge renderers). This projector only handles the invokable file-router shape.
/// </para>
/// <para>
/// Precedence note (Copilot custom-instructions docs): personal &gt; repository &gt; organization.
/// DNA emits at repository scope by default (per <see cref="ProjectionContext.TargetScope"/>=Repo);
/// no opt-in user scope is offered today because Copilot's per-user instructions live in
/// IDE-managed settings, not a filesystem path the projector can write.
/// </para>
/// </remarks>
public sealed class CopilotSkillProjector : ISkillProjector
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
    public string ClientKind => "copilot";

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
                        Message: $"Removed Claude-Code superset field '{field}' from Copilot projection " +
                                 "(Copilot ignores Claude Code-specific fields; path-scoped variants belong " +
                                 "under .github/instructions/ via SUC-07)."));
                }
            }
        }

        var body = SkillMarkdownLinkRewriter.RewriteFromCanonical(
            manifest.Body,
            targetDepthFromRepoRoot: 4);
        var contents = $"---\n{frontmatter}\n---\n{body}";
        var targetPath = $".github/copilot/skills/{manifest.Name}";

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
