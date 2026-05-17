using System.Text.RegularExpressions;

namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Loads a canonical <see cref="SkillManifest"/> from a <c>dna-skills/&lt;domain&gt;/&lt;name&gt;/SKILL.md</c>
/// directory.
/// </summary>
/// <remarks>
/// Frontmatter parsing is line-oriented (mirroring <c>scripts/Test-DnaAgentSkills.ps1</c>); structured
/// access lands in a future story once the projection layer needs to read individual fields rather
/// than treat the frontmatter as an opaque block.
/// </remarks>
public static class SkillManifestLoader
{
    private static readonly Regex FrontmatterRe = new(
        // Open fence: `---` + optional horizontal whitespace + exactly one newline.
        // Close fence: same shape, captured separately so a blank line after the closing
        // fence is preserved into the body (the projector must reproduce SKILL.md byte-for-byte).
        @"\A---[\t ]*\r?\n(?<fm>.*?)\r?\n---[\t ]*\r?\n(?<body>.*)\z",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex NameRe = new(
        @"^\s*name:\s*(?<v>.+?)\s*$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>
    /// Read and parse <c>SKILL.md</c> from <paramref name="canonicalDirectory"/>.
    /// </summary>
    /// <exception cref="FileNotFoundException">SKILL.md missing.</exception>
    /// <exception cref="InvalidDataException">Frontmatter malformed (no closing <c>---</c>) or <c>name:</c> missing.</exception>
    public static SkillManifest Load(string canonicalDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalDirectory);

        var skillPath = Path.Combine(canonicalDirectory, "SKILL.md");
        if (!File.Exists(skillPath))
        {
            throw new FileNotFoundException($"SKILL.md not found in canonical directory: {canonicalDirectory}", skillPath);
        }

        var raw = File.ReadAllText(skillPath);
        var match = FrontmatterRe.Match(raw);
        if (!match.Success)
        {
            throw new InvalidDataException($"SKILL.md at '{skillPath}' is missing a closing '---' frontmatter delimiter.");
        }

        var frontmatter = match.Groups["fm"].Value;
        var body = match.Groups["body"].Value;

        var nameMatch = NameRe.Match(frontmatter);
        if (!nameMatch.Success)
        {
            throw new InvalidDataException($"SKILL.md at '{skillPath}' is missing required 'name:' frontmatter field.");
        }

        var name = nameMatch.Groups["v"].Value.Trim().Trim('"').Trim('\'');

        return new SkillManifest(
            Name: name,
            FrontmatterRaw: frontmatter,
            Body: body,
            CanonicalDirectory: canonicalDirectory);
    }
}
