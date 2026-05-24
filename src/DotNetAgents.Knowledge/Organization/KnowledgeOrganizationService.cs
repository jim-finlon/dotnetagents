// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Knowledge.Helpers;
using DotNetAgents.Knowledge.Models;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Knowledge.Organization;

/// <summary>
/// Organizes, deduplicates, and cleans up global knowledge items.
/// </summary>
public sealed class KnowledgeOrganizationService : IKnowledgeOrganizationService
{
    private readonly IKnowledgeRepository _repository;
    private readonly ILogger<KnowledgeOrganizationService>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnowledgeOrganizationService"/> class.
    /// </summary>
    /// <param name="repository">The knowledge repository.</param>
    /// <param name="logger">Optional logger.</param>
    public KnowledgeOrganizationService(
        IKnowledgeRepository repository,
        ILogger<KnowledgeOrganizationService>? logger = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<KnowledgeOrganizationResult> OrganizeAsync(
        bool dryRun,
        bool mergeSimilar = true,
        bool organizeByCategory = true,
        CancellationToken cancellationToken = default)
    {
        var result = new KnowledgeOrganizationResult { DryRun = dryRun };
        var allItems = (await _repository.GetGlobalKnowledgeAsync(cancellationToken).ConfigureAwait(false)).ToList();

        if (allItems.Count == 0)
            return result;

        // Step 1: Find and merge exact duplicates (by content hash)
        var duplicateGroups = FindDuplicateGroups(allItems);
        foreach (var group in duplicateGroups)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (group.Count < 2)
                continue;

            var primary = group.OrderByDescending(k => k.ReferenceCount)
                .ThenByDescending(k => k.LastReferencedAt ?? k.CreatedAt)
                .First();
            var toMerge = group.Where(k => k.Id != primary.Id).ToList();

            if (!dryRun)
            {
                var mergedTags = primary.Tags.Union(toMerge.SelectMany(k => k.Tags), StringComparer.OrdinalIgnoreCase).ToList();
                var mergedTech = primary.TechStack.Union(toMerge.SelectMany(k => k.TechStack), StringComparer.OrdinalIgnoreCase).ToList();
                var totalRef = primary.ReferenceCount + toMerge.Sum(k => k.ReferenceCount);
                var updated = primary with
                {
                    Tags = mergedTags,
                    TechStack = mergedTech,
                    ReferenceCount = totalRef,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    LastReferencedAt = DateTimeOffset.UtcNow
                };
                await _repository.UpdateKnowledgeAsync(updated, cancellationToken).ConfigureAwait(false);
                foreach (var k in toMerge)
                {
                    await _repository.DeleteKnowledgeAsync(k.Id, cancellationToken).ConfigureAwait(false);
                    result.DeletedKnowledgeIds.Add(k.Id);
                }
            }

            result.Merged++;
            result.Merges.Add(new KnowledgeMergeOperation
            {
                PrimaryKnowledgeId = primary.Id,
                MergedKnowledgeIds = toMerge.Select(k => k.Id).ToList(),
                Reason = "Exact duplicate (same content hash)"
            });
        }

        // Step 2: Find and merge similar items (fuzzy)
        if (mergeSimilar)
        {
            var remaining = dryRun
                ? allItems
                : (await _repository.GetGlobalKnowledgeAsync(cancellationToken).ConfigureAwait(false))
                    .Where(k => !result.DeletedKnowledgeIds.Contains(k.Id))
                    .ToList();
            var similarGroups = FindSimilarGroups(remaining);
            foreach (var group in similarGroups)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (group.Count < 2)
                    continue;

                var primary = group.OrderByDescending(k => k.ReferenceCount)
                    .ThenByDescending(k => k.LastReferencedAt ?? k.CreatedAt)
                    .First();
                var toMerge = group.Where(k => k.Id != primary.Id).ToList();

                if (!dryRun)
                {
                    var mergedTags = primary.Tags.Union(toMerge.SelectMany(k => k.Tags), StringComparer.OrdinalIgnoreCase).ToList();
                    var mergedTech = primary.TechStack.Union(toMerge.SelectMany(k => k.TechStack), StringComparer.OrdinalIgnoreCase).ToList();
                    var totalRef = primary.ReferenceCount + toMerge.Sum(k => k.ReferenceCount);
                    var desc = primary.Description;
                    foreach (var k in toMerge)
                    {
                        if (k.Description.Length > primary.Description.Length * 1.2)
                            desc = $"{primary.Description}\n\nAdditional context: {k.Description}";
                    }
                    var updated = primary with
                    {
                        Tags = mergedTags,
                        TechStack = mergedTech,
                        ReferenceCount = totalRef,
                        Description = desc,
                        UpdatedAt = DateTimeOffset.UtcNow,
                        LastReferencedAt = DateTimeOffset.UtcNow
                    };
                    await _repository.UpdateKnowledgeAsync(updated, cancellationToken).ConfigureAwait(false);
                    foreach (var k in toMerge)
                    {
                        await _repository.DeleteKnowledgeAsync(k.Id, cancellationToken).ConfigureAwait(false);
                        result.DeletedKnowledgeIds.Add(k.Id);
                    }
                }

                result.Merged++;
                result.Merges.Add(new KnowledgeMergeOperation
                {
                    PrimaryKnowledgeId = primary.Id,
                    MergedKnowledgeIds = toMerge.Select(k => k.Id).ToList(),
                    Reason = "Similar content (fuzzy match)"
                });
            }
        }

        // Step 3: Organize by category (populate missing tags/tech stack)
        if (organizeByCategory)
        {
            var toOrganize = dryRun
                ? allItems
                : (await _repository.GetGlobalKnowledgeAsync(cancellationToken).ConfigureAwait(false)).ToList();
            foreach (var item in toOrganize)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (result.DeletedKnowledgeIds.Contains(item.Id))
                    continue;

                var tags = item.Tags.ToList();
                var techStack = item.TechStack.ToList();
                if (techStack.Count == 0)
                    AddTechStackHints(item, techStack);
                if (tags.Count == 0 && !string.IsNullOrWhiteSpace(item.Description))
                    AddTagsFromContent(item, tags);

                if (tags.Count != item.Tags.Count || techStack.Count != item.TechStack.Count)
                {
                    if (!dryRun)
                    {
                        var updated = item with
                        {
                            Tags = tags,
                            TechStack = techStack,
                            UpdatedAt = DateTimeOffset.UtcNow
                        };
                        await _repository.UpdateKnowledgeAsync(updated, cancellationToken).ConfigureAwait(false);
                    }
                    result.Organized++;
                }
            }
        }

        result.Deleted = result.DeletedKnowledgeIds.Count;
        _logger?.LogInformation(
            "Organize complete: {Merged} merges, {Deleted} deleted, {Organized} organized (dryRun={DryRun})",
            result.Merged,
            result.Deleted,
            result.Organized,
            dryRun);

        return result;
    }

    #region Helpers

    private static List<List<KnowledgeItem>> FindDuplicateGroups(List<KnowledgeItem> items)
    {
        var groups = new Dictionary<string, List<KnowledgeItem>>();
        foreach (var item in items)
        {
            var hash = string.IsNullOrEmpty(item.ContentHash)
                ? ContentHashHelper.CalculateContentHash(item.Title, item.Description)
                : item.ContentHash;
            if (!groups.ContainsKey(hash))
                groups[hash] = new List<KnowledgeItem>();
            groups[hash].Add(item);
        }
        return groups.Values.Where(g => g.Count > 1).ToList();
    }

    private static List<List<KnowledgeItem>> FindSimilarGroups(List<KnowledgeItem> items)
    {
        var groups = new List<List<KnowledgeItem>>();
        var processed = new HashSet<Guid>();
        var list = items.ToList();
        for (var i = 0; i < list.Count; i++)
        {
            if (processed.Contains(list[i].Id))
                continue;
            var group = new List<KnowledgeItem> { list[i] };
            processed.Add(list[i].Id);
            for (var j = i + 1; j < list.Count; j++)
            {
                if (processed.Contains(list[j].Id))
                    continue;
                if (AreSimilar(list[i], list[j]))
                {
                    group.Add(list[j]);
                    processed.Add(list[j].Id);
                }
            }
            if (group.Count > 1)
                groups.Add(group);
        }
        return groups;
    }

    private static bool AreSimilar(KnowledgeItem a, KnowledgeItem b)
    {
        if (!string.Equals(a.Title, b.Title, StringComparison.OrdinalIgnoreCase))
            return false;
        var d1 = a.Description.Length > 0 ? a.Description.Substring(0, Math.Min(200, a.Description.Length)) : "";
        var d2 = b.Description.Length > 0 ? b.Description.Substring(0, Math.Min(200, b.Description.Length)) : "";
        if (string.Equals(d1, d2, StringComparison.OrdinalIgnoreCase))
            return true;
        var w1 = d1.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var w2 = d2.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var common = w1.Intersect(w2, StringComparer.OrdinalIgnoreCase).Count();
        var total = Math.Max(w1.Length, w2.Length);
        return total > 0 && (double)common / total > 0.7;
    }

    private static void AddTechStackHints(KnowledgeItem item, List<string> techStack)
    {
        var content = $"{item.Title} {item.Description} {item.Context} {item.Solution}".ToLowerInvariant();
        var patterns = new Dictionary<string, string[]>
        {
            { "dotnet", new[] { "dotnet", ".net", "net10", "net9", "csharp", "c#" } },
            { "aspnet-core", new[] { "aspnet", "asp.net", "minimal api", "webapi" } },
            { "postgresql", new[] { "postgresql", "postgres", "npgsql" } },
            { "entity-framework", new[] { "entity framework", "ef core", "efcore" } },
            { "docker", new[] { "docker", "dockerfile", "docker-compose" } },
            { "mcp", new[] { "mcp", "model context protocol" } },
            { "serilog", new[] { "serilog" } }
        };
        foreach (var (tech, keywords) in patterns)
        {
            if (keywords.Any(k => content.Contains(k, StringComparison.OrdinalIgnoreCase)) &&
                !techStack.Contains(tech, StringComparer.OrdinalIgnoreCase))
                techStack.Add(tech);
        }
    }

    private static void AddTagsFromContent(KnowledgeItem item, List<string> tags)
    {
        var content = $"{item.Title} {item.Description}".ToLowerInvariant();
        var tagPatterns = new[] { "async", "database", "api", "mcp", "validation", "error", "performance", "security", "configuration", "testing", "docker", "git", "repository" };
        foreach (var tag in tagPatterns)
        {
            if (content.Contains(tag, StringComparison.OrdinalIgnoreCase) && !tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                tags.Add(tag);
        }
        var catTag = item.Category.ToString().ToLowerInvariant();
        if (!tags.Contains(catTag, StringComparer.OrdinalIgnoreCase))
            tags.Add(catTag);
    }

    #endregion
}
