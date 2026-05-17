using DotNetAgents.Abstractions.Exceptions;
using DotNetAgents.Knowledge.Helpers;
using DotNetAgents.Knowledge.Models;
using DotNetAgents.Knowledge.Storage;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Knowledge;

/// <summary>
/// Default implementation of <see cref="IKnowledgeRepository"/>.
/// </summary>
public class KnowledgeRepository : IKnowledgeRepository
{
    private readonly IKnowledgeStore _knowledgeStore;
    private readonly ILogger<KnowledgeRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnowledgeRepository"/> class.
    /// </summary>
    /// <param name="knowledgeStore">The knowledge store.</param>
    /// <param name="logger">The logger.</param>
    public KnowledgeRepository(IKnowledgeStore knowledgeStore, ILogger<KnowledgeRepository> logger)
    {
        _knowledgeStore = knowledgeStore ?? throw new ArgumentNullException(nameof(knowledgeStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<KnowledgeItem> AddKnowledgeAsync(KnowledgeItem knowledge, CancellationToken cancellationToken = default)
    {
        if (knowledge == null)
            throw new ArgumentNullException(nameof(knowledge));

        if (string.IsNullOrWhiteSpace(knowledge.Title))
            throw new ArgumentException("Knowledge title cannot be null or whitespace.", nameof(knowledge));

        if (string.IsNullOrWhiteSpace(knowledge.Description))
            throw new ArgumentException("Knowledge description cannot be null or whitespace.", nameof(knowledge));

        try
        {
            _logger.LogDebug("Adding knowledge item. Title: {Title}, SessionId: {SessionId}", knowledge.Title, knowledge.SessionId);

            var addedKnowledge = await _knowledgeStore.CreateAsync(knowledge, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Knowledge item added. KnowledgeId: {KnowledgeId}, Title: {Title}, Category: {Category}",
                addedKnowledge.Id,
                addedKnowledge.Title,
                addedKnowledge.Category);

            return addedKnowledge;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add knowledge item. Title: {Title}", knowledge.Title);
            throw new AgentException(
                $"Failed to add knowledge item: {ex.Message}",
                ErrorCategory.ConfigurationError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<KnowledgeItem?> GetKnowledgeAsync(Guid knowledgeId, CancellationToken cancellationToken = default)
    {
        if (knowledgeId == default)
            throw new ArgumentException("Knowledge ID cannot be default.", nameof(knowledgeId));

        try
        {
            _logger.LogDebug("Getting knowledge item {KnowledgeId}", knowledgeId);

            var knowledge = await _knowledgeStore.GetByIdAsync(knowledgeId, cancellationToken).ConfigureAwait(false);

            if (knowledge == null)
            {
                _logger.LogWarning("Knowledge item {KnowledgeId} not found", knowledgeId);
            }

            return knowledge;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get knowledge item {KnowledgeId}", knowledgeId);
            throw new AgentException(
                $"Failed to get knowledge item: {ex.Message}",
                ErrorCategory.ConfigurationError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<KnowledgeItem> UpdateKnowledgeAsync(KnowledgeItem knowledge, CancellationToken cancellationToken = default)
    {
        if (knowledge == null)
            throw new ArgumentNullException(nameof(knowledge));

        if (knowledge.Id == default)
            throw new ArgumentException("Knowledge ID cannot be default.", nameof(knowledge));

        try
        {
            _logger.LogDebug("Updating knowledge item {KnowledgeId}", knowledge.Id);

            // Verify knowledge exists
            var existingKnowledge = await _knowledgeStore.GetByIdAsync(knowledge.Id, cancellationToken).ConfigureAwait(false);
            if (existingKnowledge == null)
            {
                throw new InvalidOperationException($"Knowledge item {knowledge.Id} not found.");
            }

            var updatedKnowledge = await _knowledgeStore.UpdateAsync(knowledge, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Knowledge item updated. KnowledgeId: {KnowledgeId}", updatedKnowledge.Id);

            return updatedKnowledge;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update knowledge item {KnowledgeId}", knowledge.Id);
            throw new AgentException(
                $"Failed to update knowledge item: {ex.Message}",
                ErrorCategory.ConfigurationError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteKnowledgeAsync(Guid knowledgeId, CancellationToken cancellationToken = default)
    {
        if (knowledgeId == default)
            throw new ArgumentException("Knowledge ID cannot be default.", nameof(knowledgeId));

        try
        {
            _logger.LogDebug("Deleting knowledge item {KnowledgeId}", knowledgeId);

            await _knowledgeStore.DeleteAsync(knowledgeId, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Knowledge item deleted. KnowledgeId: {KnowledgeId}", knowledgeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete knowledge item {KnowledgeId}", knowledgeId);
            throw new AgentException(
                $"Failed to delete knowledge item: {ex.Message}",
                ErrorCategory.ConfigurationError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<KnowledgeItem>> GetKnowledgeBySessionAsync(
        string? sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting knowledge items for session {SessionId}", sessionId ?? "global");

            var knowledge = await _knowledgeStore.GetBySessionIdAsync(sessionId, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Retrieved {Count} knowledge items for session {SessionId}",
                knowledge.Count,
                sessionId ?? "global");

            return knowledge;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get knowledge items for session {SessionId}", sessionId);
            throw new AgentException(
                $"Failed to get knowledge items: {ex.Message}",
                ErrorCategory.ConfigurationError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<PagedResult<KnowledgeItem>> QueryKnowledgeAsync(
        KnowledgeQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        try
        {
            _logger.LogDebug(
                "Querying knowledge items. SessionId: {SessionId}, Category: {Category}, Page: {Page}",
                query.SessionId,
                query.Category,
                query.Page);

            var result = await _knowledgeStore.QueryAsync(query, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Query returned {Count} knowledge items (Total: {Total}, Page: {Page}/{TotalPages})",
                result.Items.Count,
                result.TotalCount,
                result.Page,
                result.TotalPages);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query knowledge items");
            throw new AgentException(
                $"Failed to query knowledge items: {ex.Message}",
                ErrorCategory.ConfigurationError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<KnowledgeItem>> SearchKnowledgeAsync(
        string searchText,
        string? sessionId = null,
        bool includeGlobal = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            throw new ArgumentException("Search text cannot be null or whitespace.", nameof(searchText));

        try
        {
            _logger.LogDebug(
                "Searching knowledge items. SearchText: {SearchText}, SessionId: {SessionId}, IncludeGlobal: {IncludeGlobal}",
                searchText,
                sessionId,
                includeGlobal);

            var results = await _knowledgeStore.SearchAsync(searchText, sessionId, includeGlobal, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Search returned {Count} knowledge items for '{SearchText}'",
                results.Count,
                searchText);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search knowledge items. SearchText: {SearchText}", searchText);
            throw new AgentException(
                $"Failed to search knowledge items: {ex.Message}",
                ErrorCategory.ConfigurationError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task IncrementReferenceCountAsync(Guid knowledgeId, CancellationToken cancellationToken = default)
    {
        if (knowledgeId == default)
            throw new ArgumentException("Knowledge ID cannot be default.", nameof(knowledgeId));

        try
        {
            _logger.LogDebug("Incrementing reference count for knowledge item {KnowledgeId}", knowledgeId);

            await _knowledgeStore.IncrementReferenceCountAsync(knowledgeId, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Reference count incremented for knowledge item {KnowledgeId}", knowledgeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment reference count for knowledge item {KnowledgeId}", knowledgeId);
            throw new AgentException(
                $"Failed to increment reference count: {ex.Message}",
                ErrorCategory.ConfigurationError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<KnowledgeItem>> GetGlobalKnowledgeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting global knowledge items");

            var knowledge = await _knowledgeStore.GetGlobalKnowledgeAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Retrieved {Count} global knowledge items", knowledge.Count);

            return knowledge;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get global knowledge items");
            throw new AgentException(
                $"Failed to get global knowledge items: {ex.Message}",
                ErrorCategory.ConfigurationError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<KnowledgeItem>> GetRelevantKnowledgeAsync(
        IReadOnlyList<string>? techStackTags,
        IReadOnlyList<string>? projectTags,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        if (maxResults <= 0)
            throw new ArgumentException("Max results must be greater than zero.", nameof(maxResults));

        try
        {
            _logger.LogDebug(
                "Getting relevant knowledge. TechStack: {TechStack}, Tags: {Tags}, MaxResults: {MaxResults}",
                techStackTags != null ? string.Join(", ", techStackTags) : "none",
                projectTags != null ? string.Join(", ", projectTags) : "none",
                maxResults);

            var knowledge = await _knowledgeStore.GetRelevantGlobalKnowledgeAsync(
                techStackTags,
                projectTags,
                maxResults,
                cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Retrieved {Count} relevant knowledge items",
                knowledge.Count);

            return knowledge;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get relevant knowledge");
            throw new AgentException(
                $"Failed to get relevant knowledge: {ex.Message}",
                ErrorCategory.ConfigurationError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<KnowledgeItem?> FindDuplicateAsync(
        string title,
        string description,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be null or whitespace.", nameof(title));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be null or whitespace.", nameof(description));

        try
        {
            _logger.LogDebug("Checking for duplicate knowledge item. Title: {Title}", title);

            var contentHash = ContentHashHelper.CalculateContentHash(title, description);
            var duplicate = await _knowledgeStore.GetByContentHashAsync(contentHash, cancellationToken).ConfigureAwait(false);

            if (duplicate != null)
            {
                _logger.LogDebug("Duplicate knowledge item found. KnowledgeId: {KnowledgeId}", duplicate.Id);
            }

            return duplicate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for duplicate knowledge item");
            throw new AgentException(
                $"Failed to check for duplicate: {ex.Message}",
                ErrorCategory.ConfigurationError,
                ex);
        }
    }
}
