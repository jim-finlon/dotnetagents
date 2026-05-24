// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Skills;

/// <summary>
/// Parsed metadata for a Skill folder. Loaded from <c>SKILL.md</c>'s YAML frontmatter +
/// folder contents enumeration.
/// </summary>
/// <param name="Id">Stable identifier (typically the folder name).</param>
/// <param name="Name">Human-readable name from frontmatter <c>name</c>.</param>
/// <param name="Description">One-paragraph description from frontmatter <c>description</c>. This is what the retriever matches against.</param>
/// <param name="Version">Optional semver string from frontmatter <c>version</c>.</param>
/// <param name="Instructions">Body of <c>SKILL.md</c> below the frontmatter — the system-prompt-equivalent instructions injected into agent context.</param>
/// <param name="ResourceFiles">Sibling files in the skill folder (any extension). Made available to the agent as resources.</param>
/// <param name="Dependencies">Optional list of skill ids this skill depends on (loaded together).</param>
/// <param name="Scripts">Optional named-script map from frontmatter <c>scripts</c>.</param>
/// <param name="FolderPath">Absolute filesystem path to the skill folder (for resource resolution).</param>
public sealed record SkillDescriptor(
    string Id,
    string Name,
    string Description,
    string? Version,
    string Instructions,
    IReadOnlyList<string> ResourceFiles,
    IReadOnlyList<string> Dependencies,
    IReadOnlyDictionary<string, string> Scripts,
    string FolderPath);
