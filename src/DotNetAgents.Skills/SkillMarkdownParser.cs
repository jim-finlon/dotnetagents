// SPDX-License-Identifier: Apache-2.0

using System.Text;

namespace DotNetAgents.Skills;

/// <summary>
/// Parses a SKILL.md file: YAML frontmatter (between two <c>---</c> lines) plus the
/// markdown body below. The frontmatter follows Anthropic's Skills convention:
/// <list type="bullet">
///   <item><description><c>name</c> (required)</description></item>
///   <item><description><c>description</c> (required) — used for retrieval matching</description></item>
///   <item><description><c>version</c> (optional)</description></item>
///   <item><description><c>dependencies</c> (optional, list)</description></item>
///   <item><description><c>scripts</c> (optional, map)</description></item>
/// </list>
/// </summary>
/// <remarks>
/// MVP parser: line-based YAML for the canonical fields. Full YAML is deferred to a
/// follow-up — operators who need complex frontmatter can switch to YamlDotNet later
/// without breaking the descriptor contract.
/// </remarks>
public static class SkillMarkdownParser
{
    /// <summary>Parse a SKILL.md file's contents into the four logical sections.</summary>
    public static ParsedSkillMarkdown Parse(string skillMarkdownContent)
    {
        ArgumentNullException.ThrowIfNull(skillMarkdownContent);

        var lines = skillMarkdownContent.Replace("\r\n", "\n").Split('\n');
        var frontmatter = ExtractFrontmatter(lines, out var bodyStartIndex);
        var body = bodyStartIndex >= 0
            ? string.Join("\n", lines.Skip(bodyStartIndex)).Trim()
            : skillMarkdownContent.Trim();

        return new ParsedSkillMarkdown(
            Name: GetString(frontmatter, "name"),
            Description: GetString(frontmatter, "description"),
            Version: GetString(frontmatter, "version"),
            Dependencies: GetList(frontmatter, "dependencies"),
            Scripts: GetMap(frontmatter, "scripts"),
            Body: body);
    }

    private static Dictionary<string, object> ExtractFrontmatter(string[] lines, out int bodyStartIndex)
    {
        bodyStartIndex = -1;
        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (lines.Length < 2 || lines[0].Trim() != "---")
        {
            return dict;
        }

        // Find closing ---
        var closingIdx = -1;
        for (var i = 1; i < lines.Length; i++)
        {
            if (lines[i].Trim() == "---")
            {
                closingIdx = i;
                break;
            }
        }
        if (closingIdx < 0) return dict;

        bodyStartIndex = closingIdx + 1;

        // Parse k:v pairs and simple structures (- list, k: v map under indented keys).
        string? currentKey = null;
        List<string>? currentList = null;
        Dictionary<string, string>? currentMap = null;

        for (var i = 1; i < closingIdx; i++)
        {
            var raw = lines[i];
            var trimmed = raw.TrimEnd();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            // List item under previous key
            if (raw.StartsWith("  - ", StringComparison.Ordinal) || raw.StartsWith("- ", StringComparison.Ordinal))
            {
                var item = trimmed.TrimStart(' ', '-', ' ').Trim();
                if (currentList is null && currentKey is not null)
                {
                    currentList = new List<string>();
                    dict[currentKey] = currentList;
                }
                currentList?.Add(item);
                currentMap = null;
                continue;
            }

            // Indented map entry (under previous top-level key)
            if (raw.StartsWith("  ", StringComparison.Ordinal) && raw.Contains(':'))
            {
                var idx = trimmed.IndexOf(':');
                var k = trimmed[..idx].Trim();
                var v = idx < trimmed.Length - 1 ? trimmed[(idx + 1)..].Trim().Trim('"', '\'') : string.Empty;
                if (currentMap is null && currentKey is not null)
                {
                    currentMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    dict[currentKey] = currentMap;
                }
                if (currentMap is not null) currentMap[k] = v;
                currentList = null;
                continue;
            }

            // Top-level k: v
            var colonIdx = trimmed.IndexOf(':');
            if (colonIdx > 0)
            {
                currentKey = trimmed[..colonIdx].Trim();
                var rest = colonIdx < trimmed.Length - 1 ? trimmed[(colonIdx + 1)..].Trim() : string.Empty;
                if (string.IsNullOrEmpty(rest))
                {
                    // Multi-line value coming on subsequent lines (list or map)
                    currentList = null;
                    currentMap = null;
                }
                else
                {
                    dict[currentKey] = rest.Trim('"', '\'');
                    currentList = null;
                    currentMap = null;
                }
            }
        }

        return dict;
    }

    private static string GetString(IReadOnlyDictionary<string, object> dict, string key) =>
        dict.TryGetValue(key, out var v) && v is string s ? s : string.Empty;

    private static IReadOnlyList<string> GetList(IReadOnlyDictionary<string, object> dict, string key) =>
        dict.TryGetValue(key, out var v) && v is List<string> list
            ? list.ToArray()
            : Array.Empty<string>();

    private static IReadOnlyDictionary<string, string> GetMap(IReadOnlyDictionary<string, object> dict, string key) =>
        dict.TryGetValue(key, out var v) && v is Dictionary<string, string> map
            ? new Dictionary<string, string>(map, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>The four logical sections of a parsed SKILL.md.</summary>
public sealed record ParsedSkillMarkdown(
    string Name,
    string Description,
    string? Version,
    IReadOnlyList<string> Dependencies,
    IReadOnlyDictionary<string, string> Scripts,
    string Body);
