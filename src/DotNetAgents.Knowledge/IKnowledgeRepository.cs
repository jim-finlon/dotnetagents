using DotNetAgents.Knowledge.Models;

namespace DotNetAgents.Knowledge;

/// <summary>
/// Interface for managing knowledge items.
/// </summary>
public interface IKnowledgeRepository
{
    /// <summary>
    /// Adds a new knowledge item.
    /// </summary>
    /// <param name="knowledge">The knowledge item to add.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The added knowledge item.</returns>
    Task<KnowledgeItem> AddKnowledgeAsync(KnowledgeItem knowledge, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a knowledge item by its unique identifier.
    /// </summary>
    /// <param name="knowledgeId">The knowledge item identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The knowledge item if found; otherwise, null.</returns>
    Task<KnowledgeItem?> GetKnowledgeAsync(Guid knowledgeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing knowledge item.
    /// </summary>
    /// <param name="knowledge">The knowledge item with updated values.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The updated knowledge item.</returns>
    Task<KnowledgeItem> UpdateKnowledgeAsync(KnowledgeItem knowledge, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a knowledge item.
    /// </summary>
    /// <param name="knowledgeId">The knowledge item identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteKnowledgeAsync(Guid knowledgeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets knowledge items for a specific session.
    /// </summary>
    /// <param name="sessionId">The session identifier (null for global knowledge).</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>List of knowledge items.</returns>
    Task<IReadOnlyList<KnowledgeItem>> GetKnowledgeBySessionAsync(
        string? sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries knowledge items with filters.
    /// </summary>
    /// <param name="query">Query parameters.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>Paginated list of knowledge items.</returns>
    Task<PagedResult<KnowledgeItem>> QueryKnowledgeAsync(
        KnowledgeQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches knowledge items by text (full-text search).
    /// </summary>
    /// <param name="searchText">Text to search for.</param>
    /// <param name="sessionId">Optional session filter.</param>
    /// <param name="includeGlobal">Include global knowledge items.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>Matching knowledge items.</returns>
    Task<IReadOnlyList<KnowledgeItem>> SearchKnowledgeAsync(
        string searchText,
        string? sessionId = null,
        bool includeGlobal = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the reference count for a knowledge item.
    /// </summary>
    /// <param name="knowledgeId">The knowledge item identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task IncrementReferenceCountAsync(Guid knowledgeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets global knowledge items (not tied to any session).
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>List of global knowledge items.</returns>
    Task<IReadOnlyList<KnowledgeItem>> GetGlobalKnowledgeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets relevant global knowledge items based on tech stack and tags.
    /// </summary>
    /// <param name="techStackTags">Tech stack tags to match against knowledge TechStack.</param>
    /// <param name="projectTags">Project tags to match against knowledge Tags.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>List of relevant global knowledge items, ordered by relevance.</returns>
    Task<IReadOnlyList<KnowledgeItem>> GetRelevantKnowledgeAsync(
        IReadOnlyList<string>? techStackTags,
        IReadOnlyList<string>? projectTags,
        int maxResults,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a knowledge item with the same content already exists (duplicate detection).
    /// </summary>
    /// <param name="title">The knowledge item title.</param>
    /// <param name="description">The knowledge item description.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The existing knowledge item if found; otherwise, null.</returns>
    Task<KnowledgeItem?> FindDuplicateAsync(
        string title,
        string description,
        CancellationToken cancellationToken = default);
}
