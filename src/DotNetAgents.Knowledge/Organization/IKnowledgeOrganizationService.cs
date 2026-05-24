// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Knowledge.Organization;

/// <summary>
/// Service for organizing, deduplicating, and cleaning up global knowledge items.
/// </summary>
public interface IKnowledgeOrganizationService
{
    /// <summary>
    /// Scans global knowledge items, finds duplicates, merges them, and optionally organizes by category/tags.
    /// </summary>
    /// <param name="dryRun">If true, returns what would be done without making changes.</param>
    /// <param name="mergeSimilar">If true, merges similar items (fuzzy matching by title and description).</param>
    /// <param name="organizeByCategory">If true, ensures tags and tech stack are populated from content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Organization result with statistics.</returns>
    Task<KnowledgeOrganizationResult> OrganizeAsync(
        bool dryRun,
        bool mergeSimilar = true,
        bool organizeByCategory = true,
        CancellationToken cancellationToken = default);
}
