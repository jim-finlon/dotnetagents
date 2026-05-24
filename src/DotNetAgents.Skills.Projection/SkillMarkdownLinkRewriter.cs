// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;

namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Rewrites parent-directory prefixes in Markdown link targets when a canonical
/// <c>dna-skills/&lt;domain&gt;/&lt;name&gt;/SKILL.md</c> file is projected deeper or shallower in the tree.
/// </summary>
public static partial class SkillMarkdownLinkRewriter
{
    /// <summary>
    /// Directory depth from repo root for canonical skills (<c>dna-skills/domain/name</c>).
    /// </summary>
    public const int CanonicalSkillDepthFromRepoRoot = 3;

    private static readonly Regex MarkdownRelativeLinkRegex = MarkdownRelativeLinkPattern();

    /// <summary>
    /// Adjust <c>../</c> prefixes in Markdown link targets for the projected file depth.
    /// </summary>
    public static string RewriteFromCanonical(string content, int targetDepthFromRepoRoot)
    {
        var delta = targetDepthFromRepoRoot - CanonicalSkillDepthFromRepoRoot;
        if (delta == 0)
        {
            return content;
        }

        return MarkdownRelativeLinkRegex.Replace(
            content,
            match =>
            {
                var prefix = match.Groups["prefix"].Value;
                var rest = match.Groups["rest"].Value;
                var newPrefix = AdjustParentPrefix(prefix, delta);
                return $"]({newPrefix}{rest}";
            });
    }

    internal static string AdjustParentPrefix(string prefix, int delta)
    {
        var count = 0;
        for (var i = 0; i < prefix.Length; i += 3)
        {
            if (i + 2 < prefix.Length && prefix[i] == '.' && prefix[i + 1] == '.' && prefix[i + 2] == '/')
            {
                count++;
                continue;
            }

            break;
        }

        var newCount = Math.Max(1, count + delta);
        return string.Concat(Enumerable.Repeat("../", newCount));
    }

    [GeneratedRegex(@"\]\((?<prefix>(?:\.\./)+)(?<rest>[^)#\s][^)]*)", RegexOptions.CultureInvariant)]
    private static partial Regex MarkdownRelativeLinkPattern();
}
