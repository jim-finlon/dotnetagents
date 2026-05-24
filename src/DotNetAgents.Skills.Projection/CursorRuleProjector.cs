// SPDX-License-Identifier: Apache-2.0

using System.Text;

namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Renders a canonical DNA skill as a Cursor rule (<c>.cursor/rules/&lt;name&gt;.mdc</c>) so a
/// skill marked <c>posture: always-on</c> (or <c>paths: …</c>) participates in Cursor's rule
/// system rather than only its skill picker.
/// </summary>
/// <remarks>
/// <para>
/// Phase-1 lossy-edge renderer per SUC-07. Behaviour per posture:
/// <list type="bullet">
///   <item><c>posture: always-on</c> → <c>alwaysApply: true</c>; no <c>globs</c>.</item>
///   <item><c>posture: path-scoped</c> with <c>paths: [glob, …]</c> → <c>alwaysApply: false</c> + <c>globs: …</c>.</item>
///   <item>Any other posture (invokable, manual, missing) → manual-reference rule
///     (<c>alwaysApply: false</c>, no <c>globs</c>, just description + body) plus a warning so the
///     orchestrator knows the rule won't auto-load.</item>
/// </list>
/// </para>
/// <para>
/// The Cursor rule body is the SKILL.md body verbatim — the rule layer treats it as plain
/// Markdown context. Cursor's <c>.mdc</c> format expects a frontmatter block with
/// <c>description</c>, <c>globs</c>, and <c>alwaysApply</c>; we keep that surface minimal so
/// re-projection stays byte-deterministic.
/// </para>
/// </remarks>
public sealed class CursorRuleProjector : ISkillProjector
{
    /// <inheritdoc />
    public string ClientKind => "cursor-rule";

    /// <inheritdoc />
    public SkillProjection Project(SkillManifest manifest, ProjectionContext context)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(context);

        var warnings = new List<SkillProjectionWarning>();
        var posture = FrontmatterFieldReader.Read(manifest.FrontmatterRaw, "posture");
        var description = FrontmatterFieldReader.Read(manifest.FrontmatterRaw, "description");

        bool alwaysApply = false;
        IReadOnlyList<string> globs = Array.Empty<string>();

        if (string.Equals(posture, "always-on", StringComparison.Ordinal))
        {
            alwaysApply = true;
        }
        else if (string.Equals(posture, "path-scoped", StringComparison.Ordinal))
        {
            // path-scoped skills declare their globs in a top-level `paths:` sequence.
            globs = FrontmatterFieldReader.ReadSequence(manifest.FrontmatterRaw, "paths");
            if (globs.Count == 0)
            {
                warnings.Add(new SkillProjectionWarning(
                    Code: "cursor_rule_missing_paths",
                    Message: $"Skill '{manifest.Name}' declares posture=path-scoped but has no 'paths:' sequence; " +
                             "Cursor rule emitted as manual-reference (alwaysApply=false, no globs)."));
            }
        }
        else if (!string.IsNullOrEmpty(posture)
                 && posture != "invokable"
                 && posture != "manual")
        {
            warnings.Add(new SkillProjectionWarning(
                Code: "cursor_rule_unknown_posture",
                Message: $"Skill '{manifest.Name}' has posture='{posture}'; Cursor rule emitted as manual-reference."));
        }

        var body = manifest.Body.TrimEnd('\n');
        var contents = RenderMdc(description, globs, alwaysApply, body);

        return new SkillProjection(
            TargetPath: ".cursor/rules",
            FileName: $"{manifest.Name}.mdc",
            Contents: contents,
            Mode: SkillProjectionMode.Write,
            Warnings: warnings);
    }

    private static string RenderMdc(string description, IReadOnlyList<string> globs, bool alwaysApply, string body)
    {
        var sb = new StringBuilder();
        sb.Append("---\n");
        sb.Append("description: ").Append(description).Append('\n');
        if (globs.Count > 0)
        {
            sb.Append("globs:\n");
            foreach (var g in globs)
            {
                sb.Append("  - ").Append(g).Append('\n');
            }
        }
        else
        {
            sb.Append("globs: []\n");
        }
        sb.Append("alwaysApply: ").Append(alwaysApply ? "true" : "false").Append('\n');
        sb.Append("---\n");
        sb.Append(body).Append('\n');
        return sb.ToString();
    }
}
