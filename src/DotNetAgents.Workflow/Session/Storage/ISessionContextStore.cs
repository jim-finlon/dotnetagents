using DotNetAgents.Workflow.Session;

namespace DotNetAgents.Workflow.Session.Storage;

/// <summary>
/// Interface for storing and retrieving session context.
/// </summary>
public interface ISessionContextStore
{
    /// <summary>
    /// Creates or updates session context (one-to-one relationship with session).
    /// </summary>
    /// <param name="context">The session context to create or update.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The created or updated context.</returns>
    Task<SessionContext> CreateOrUpdateAsync(SessionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets session context by session identifier.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The session context if found; otherwise, null.</returns>
    Task<SessionContext?> GetBySessionIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes session context.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default);
}
