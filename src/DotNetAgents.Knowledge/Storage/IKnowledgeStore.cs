using DotNetAgents.Knowledge.Models;

namespace DotNetAgents.Knowledge.Storage;

/// <summary>
/// Interface for storing and retrieving knowledge items.
/// </summary>
public interface IKnowledgeStore
{
    /// <summary>
    /// Gets a knowledge item by its unique identifier.
    /// </summary>
    /// <param name="knowledgeId">The knowledge item identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The knowledge item if found; otherwise, null.</returns>
    Task<KnowledgeItem?> GetByIdAsync(Guid knowledgeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new knowledge item.
    /// </summary>
    /// <param name="knowledge">The knowledge item to create.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The created knowledge item with generated ID.</returns>
    Task<KnowledgeItem> CreateAsync(KnowledgeItem knowledge, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing knowledge item.
    /// </summary>
    /// <param name="knowledge">The knowledge item with updated values.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The updated knowledge item.</returns>
    Task<KnowledgeItem> UpdateAsync(KnowledgeItem knowledge, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a knowledge item.
    /// </summary>
    /// <param name="knowledgeId">The knowledge item identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(Guid knowledgeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets knowledge items for a specific session.
    /// </summary>
    /// <param name="sessionId">The session identifier (null for global knowledge).</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>List of knowledge items.</returns>
    Task<IReadOnlyList<KnowledgeItem>> GetBySessionIdAsync(
        string? sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries knowledge items with filters.
    /// </summary>
    /// <param name="query">Query parameters.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>Paginated list of knowledge items.</returns>
    Task<PagedResult<KnowledgeItem>> QueryAsync(
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
    Task<IReadOnlyList<KnowledgeItem>> SearchAsync(
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
    Task<IReadOnlyList<KnowledgeItem>> GetRelevantGlobalKnowledgeAsync(
        IReadOnlyList<string>? techStackTags,
        IReadOnlyList<string>? projectTags,
        int maxResults,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a knowledge item by content hash for fast duplicate detection.
    /// </summary>
    /// <param name="contentHash">SHA256 hash of title + description.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The knowledge item if found; otherwise, null.</returns>
    Task<KnowledgeItem?> GetByContentHashAsync(
        string contentHash,
        CancellationToken cancellationToken = default);
}
