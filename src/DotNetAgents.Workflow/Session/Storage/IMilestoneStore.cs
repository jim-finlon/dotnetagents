using DotNetAgents.Workflow.Session;

namespace DotNetAgents.Workflow.Session.Storage;

/// <summary>
/// Interface for storing and retrieving milestones.
/// </summary>
public interface IMilestoneStore
{
    /// <summary>
    /// Creates a new milestone.
    /// </summary>
    /// <param name="milestone">The milestone to create.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The created milestone.</returns>
    Task<Milestone> CreateAsync(Milestone milestone, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a milestone by its unique identifier.
    /// </summary>
    /// <param name="milestoneId">The milestone identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The milestone if found; otherwise, null.</returns>
    Task<Milestone?> GetByIdAsync(Guid milestoneId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing milestone.
    /// </summary>
    /// <param name="milestone">The milestone with updated values.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The updated milestone.</returns>
    Task<Milestone> UpdateAsync(Milestone milestone, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all milestones for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>List of milestones ordered by order property.</returns>
    Task<IReadOnlyList<Milestone>> GetBySessionIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a milestone.
    /// </summary>
    /// <param name="milestoneId">The milestone identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(Guid milestoneId, CancellationToken cancellationToken = default);
}
