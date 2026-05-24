// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Skills;

/// <summary>
/// Discovery + lookup for Skills. Loads SKILL.md descriptors from configured directories;
/// caches them by id; supports manual reload for filesystem changes.
/// </summary>
public interface ISkillRegistry
{
    /// <summary>All skills currently in the registry, ordered by id.</summary>
    IReadOnlyList<SkillDescriptor> All();

    /// <summary>Look up a skill by id. Returns null when not found.</summary>
    SkillDescriptor? GetById(string id);

    /// <summary>
    /// Re-scan the configured directories and rebuild the registry. Existing descriptors are
    /// replaced atomically — concurrent <see cref="All"/> / <see cref="GetById"/> callers see
    /// either the old or new state, never a partial view.
    /// </summary>
    Task ReloadAsync(CancellationToken cancellationToken = default);

    /// <summary>The directories the registry scans for skill folders.</summary>
    IReadOnlyList<string> ScanDirectories { get; }
}

/// <summary>
/// Match a task description against the registry's skills and return the top-K most relevant
/// descriptors with similarity scores.
/// </summary>
public interface ISkillRetriever
{
    /// <summary>
    /// Match <paramref name="taskDescription"/> against all skills and return the top
    /// <paramref name="topK"/> by similarity score. Higher scores are more relevant.
    /// </summary>
    IReadOnlyList<SkillMatch> Match(string taskDescription, int topK = 3);
}

/// <summary>One result from <see cref="ISkillRetriever.Match"/>: skill + similarity.</summary>
public sealed record SkillMatch(SkillDescriptor Skill, double Score);
