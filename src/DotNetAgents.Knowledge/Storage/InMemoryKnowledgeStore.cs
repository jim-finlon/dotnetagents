using DotNetAgents.Knowledge.Helpers;
using DotNetAgents.Knowledge.Models;

namespace DotNetAgents.Knowledge.Storage;

/// <summary>
/// In-memory implementation of <see cref="IKnowledgeStore"/> for testing and development.
/// </summary>
public class InMemoryKnowledgeStore : IKnowledgeStore
{
    private readonly Dictionary<Guid, KnowledgeItem> _knowledge = new();
    private readonly Dictionary<string, List<Guid>> _sessionKnowledge = new();
    private readonly Dictionary<string, Guid> _contentHashIndex = new();
    private readonly object _lock = new();

    /// <inheritdoc/>
    public Task<KnowledgeItem?> GetByIdAsync(Guid knowledgeId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _knowledge.TryGetValue(knowledgeId, out var item);
            return Task.FromResult<KnowledgeItem?>(item);
        }
    }

    /// <inheritdoc/>
    public Task<KnowledgeItem> CreateAsync(KnowledgeItem knowledge, CancellationToken cancellationToken = default)
    {
        if (knowledge == null)
            throw new ArgumentNullException(nameof(knowledge));

        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            // Calculate content hash if not provided
            var contentHash = knowledge.ContentHash;
            if (string.IsNullOrWhiteSpace(contentHash))
            {
                contentHash = ContentHashHelper.CalculateContentHash(knowledge.Title, knowledge.Description);
            }

            var knowledgeToCreate = knowledge with
            {
                Id = knowledge.Id == default ? Guid.NewGuid() : knowledge.Id,
                CreatedAt = knowledge.CreatedAt == default ? DateTimeOffset.UtcNow : knowledge.CreatedAt,
                UpdatedAt = DateTimeOffset.UtcNow,
                ContentHash = contentHash
            };

            _knowledge[knowledgeToCreate.Id] = knowledgeToCreate;

            // Track by session
            var sessionKey = knowledgeToCreate.SessionId ?? "global";
            if (!_sessionKnowledge.ContainsKey(sessionKey))
            {
                _sessionKnowledge[sessionKey] = new List<Guid>();
            }
            _sessionKnowledge[sessionKey].Add(knowledgeToCreate.Id);

            // Index by content hash
            if (!string.IsNullOrWhiteSpace(contentHash))
            {
                _contentHashIndex[contentHash] = knowledgeToCreate.Id;
            }

            return Task.FromResult(knowledgeToCreate);
        }
    }

    /// <inheritdoc/>
    public Task<KnowledgeItem> UpdateAsync(KnowledgeItem knowledge, CancellationToken cancellationToken = default)
    {
        if (knowledge == null)
            throw new ArgumentNullException(nameof(knowledge));

        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (!_knowledge.ContainsKey(knowledge.Id))
            {
                throw new InvalidOperationException($"Knowledge item {knowledge.Id} not found.");
            }

            var updatedKnowledge = knowledge with
            {
                UpdatedAt = DateTimeOffset.UtcNow,
                LastReferencedAt = knowledge.LastReferencedAt ?? _knowledge[knowledge.Id].LastReferencedAt
            };

            _knowledge[knowledge.Id] = updatedKnowledge;
            return Task.FromResult(updatedKnowledge);
        }
    }

    /// <inheritdoc/>
    public Task DeleteAsync(Guid knowledgeId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (!_knowledge.TryGetValue(knowledgeId, out var knowledge))
            {
                return Task.CompletedTask;
            }

            _knowledge.Remove(knowledgeId);

            // Remove from session tracking
            var sessionKey = knowledge.SessionId ?? "global";
            if (_sessionKnowledge.TryGetValue(sessionKey, out var sessionKnowledgeIds))
            {
                sessionKnowledgeIds.Remove(knowledgeId);
                if (sessionKnowledgeIds.Count == 0)
                {
                    _sessionKnowledge.Remove(sessionKey);
                }
            }

            // Remove from content hash index
            if (!string.IsNullOrWhiteSpace(knowledge.ContentHash) &&
                _contentHashIndex.TryGetValue(knowledge.ContentHash, out var indexedId) &&
                indexedId == knowledgeId)
            {
                _contentHashIndex.Remove(knowledge.ContentHash);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<KnowledgeItem>> GetBySessionIdAsync(
        string? sessionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var sessionKey = sessionId ?? "global";
            if (!_sessionKnowledge.TryGetValue(sessionKey, out var knowledgeIds))
            {
                return Task.FromResult<IReadOnlyList<KnowledgeItem>>(Array.Empty<KnowledgeItem>());
            }

            var items = knowledgeIds
                .Select(id => _knowledge.TryGetValue(id, out var item) ? item : null)
                .Where(item => item != null)
                .Cast<KnowledgeItem>()
                .OrderByDescending(item => item.CreatedAt)
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<KnowledgeItem>>(items);
        }
    }

    /// <inheritdoc/>
    public Task<PagedResult<KnowledgeItem>> QueryAsync(
        KnowledgeQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var allItems = GetAllKnowledgeItems().ToList();

            // Filter by session
            if (!string.IsNullOrWhiteSpace(query.SessionId))
            {
                if (query.IncludeGlobal)
                {
                    allItems = allItems.Where(k => k.SessionId == query.SessionId || k.SessionId == null).ToList();
                }
                else
                {
                    allItems = allItems.Where(k => k.SessionId == query.SessionId).ToList();
                }
            }
            else if (!query.IncludeGlobal)
            {
                allItems = allItems.Where(k => k.SessionId != null).ToList();
            }

            // Filter by category
            if (query.Category.HasValue)
            {
                allItems = allItems.Where(k => k.Category == query.Category.Value).ToList();
            }

            // Filter by severity
            if (query.Severity.HasValue)
            {
                allItems = allItems.Where(k => k.Severity == query.Severity.Value).ToList();
            }

            // Filter by tags (OR logic - any matching tag)
            if (query.Tags != null && query.Tags.Count > 0)
            {
                allItems = allItems.Where(k => k.Tags.Any(t => query.Tags!.Contains(t))).ToList();
            }

            // Full-text search
            if (!string.IsNullOrWhiteSpace(query.SearchText))
            {
                var searchLower = query.SearchText.ToLowerInvariant();
                allItems = allItems.Where(k =>
                    k.Title.ToLowerInvariant().Contains(searchLower) ||
                    k.Description.ToLowerInvariant().Contains(searchLower) ||
                    (k.Context != null && k.Context.ToLowerInvariant().Contains(searchLower)) ||
                    (k.Solution != null && k.Solution.ToLowerInvariant().Contains(searchLower))).ToList();
            }

            // Sort
            var sortedItems = query.SortDescending
                ? allItems.OrderByDescending(k => GetSortValue(k, query.SortBy)).ToList()
                : allItems.OrderBy(k => GetSortValue(k, query.SortBy)).ToList();

            // Pagination
            var totalCount = sortedItems.Count;
            var skip = (query.Page - 1) * query.PageSize;
            var items = sortedItems.Skip(skip).Take(query.PageSize).ToList().AsReadOnly();

            var result = new PagedResult<KnowledgeItem>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };

            return Task.FromResult(result);
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<KnowledgeItem>> SearchAsync(
        string searchText,
        string? sessionId = null,
        bool includeGlobal = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            throw new ArgumentException("Search text cannot be null or whitespace.", nameof(searchText));

        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var allItems = GetAllKnowledgeItems().ToList();

            // Filter by session
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                if (includeGlobal)
                {
                    allItems = allItems.Where(k => k.SessionId == sessionId || k.SessionId == null).ToList();
                }
                else
                {
                    allItems = allItems.Where(k => k.SessionId == sessionId).ToList();
                }
            }
            else if (!includeGlobal)
            {
                allItems = allItems.Where(k => k.SessionId != null).ToList();
            }

            // Full-text search
            var searchLower = searchText.ToLowerInvariant();
            var results = allItems
                .Where(k =>
                    k.Title.ToLowerInvariant().Contains(searchLower) ||
                    k.Description.ToLowerInvariant().Contains(searchLower) ||
                    (k.Context != null && k.Context.ToLowerInvariant().Contains(searchLower)) ||
                    (k.Solution != null && k.Solution.ToLowerInvariant().Contains(searchLower)))
                .OrderByDescending(k => k.ReferenceCount)
                .ThenByDescending(k => k.CreatedAt)
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<KnowledgeItem>>(results);
        }
    }

    /// <inheritdoc/>
    public Task IncrementReferenceCountAsync(Guid knowledgeId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (!_knowledge.TryGetValue(knowledgeId, out var knowledge))
            {
                return Task.CompletedTask;
            }

            var updated = knowledge with
            {
                ReferenceCount = knowledge.ReferenceCount + 1,
                LastReferencedAt = DateTimeOffset.UtcNow
            };

            _knowledge[knowledgeId] = updated;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<KnowledgeItem>> GetGlobalKnowledgeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var globalItems = GetAllKnowledgeItems()
                .Where(k => k.SessionId == null)
                .OrderByDescending(k => k.ReferenceCount)
                .ThenByDescending(k => k.CreatedAt)
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<KnowledgeItem>>(globalItems);
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<KnowledgeItem>> GetRelevantGlobalKnowledgeAsync(
        IReadOnlyList<string>? techStackTags,
        IReadOnlyList<string>? projectTags,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var allGlobalItems = GetAllKnowledgeItems()
                .Where(k => k.SessionId == null)
                .OrderByDescending(k => k.ReferenceCount)
                .ThenByDescending(k => k.LastReferencedAt ?? k.CreatedAt)
                .Take(maxResults * 3) // Get 3x candidates, then score top N
                .ToList();

            // Score relevance for each candidate
            var scoredItems = allGlobalItems
                .Select(knowledge =>
                {
                    var score = 0;

                    // Tech stack matching
                    if (techStackTags != null && techStackTags.Count > 0 && knowledge.TechStack.Count > 0)
                    {
                        var matchingTech = knowledge.TechStack.Intersect(techStackTags, StringComparer.OrdinalIgnoreCase).Count();
                        score += matchingTech * 10; // Tech stack matches are worth more
                    }

                    // Tag matching
                    if (projectTags != null && projectTags.Count > 0 && knowledge.Tags.Count > 0)
                    {
                        var matchingTags = knowledge.Tags.Intersect(projectTags, StringComparer.OrdinalIgnoreCase).Count();
                        score += matchingTags * 5;
                    }

                    // Severity weight
                    score += knowledge.Severity switch
                    {
                        KnowledgeSeverity.Critical => 20,
                        KnowledgeSeverity.Error => 15,
                        KnowledgeSeverity.Warning => 10,
                        _ => 5
                    };

                    // Reference count boost
                    score += Math.Min(knowledge.ReferenceCount, 10); // Cap at 10

                    return new { Knowledge = knowledge, Score = score };
                })
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Knowledge.ReferenceCount)
                .ThenByDescending(x => x.Knowledge.LastReferencedAt ?? x.Knowledge.CreatedAt)
                .Take(maxResults)
                .Select(x => x.Knowledge)
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<KnowledgeItem>>(scoredItems);
        }
    }

    /// <inheritdoc/>
    public Task<KnowledgeItem?> GetByContentHashAsync(
        string contentHash,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contentHash))
            throw new ArgumentException("Content hash cannot be null or whitespace.", nameof(contentHash));

        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (!_contentHashIndex.TryGetValue(contentHash, out var knowledgeId))
            {
                return Task.FromResult<KnowledgeItem?>(null);
            }

            _knowledge.TryGetValue(knowledgeId, out var knowledge);
            return Task.FromResult<KnowledgeItem?>(knowledge);
        }
    }

    private IEnumerable<KnowledgeItem> GetAllKnowledgeItems()
    {
        return _knowledge.Values;
    }

    private static object GetSortValue(KnowledgeItem knowledge, string sortBy)
    {
        return sortBy switch
        {
            "CreatedAt" => knowledge.CreatedAt,
            "ReferenceCount" => knowledge.ReferenceCount,
            "UpdatedAt" => knowledge.UpdatedAt,
            _ => knowledge.CreatedAt
        };
    }
}
