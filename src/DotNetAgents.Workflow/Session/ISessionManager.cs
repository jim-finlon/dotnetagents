using DotNetAgents.Workflow.Session.Bootstrap;

namespace DotNetAgents.Workflow.Session;

/// <summary>
/// Interface for managing workflow sessions, snapshots, and milestones.
/// </summary>
public interface ISessionManager
{
    /// <summary>
    /// Creates a snapshot of the current workflow state.
    /// </summary>
    /// <param name="snapshot">The snapshot to create.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The created snapshot.</returns>
    Task<WorkflowSnapshot> CreateSnapshotAsync(WorkflowSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a snapshot by its unique identifier.
    /// </summary>
    /// <param name="snapshotId">The snapshot identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The snapshot if found; otherwise, null.</returns>
    Task<WorkflowSnapshot?> GetSnapshotAsync(Guid snapshotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all snapshots for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>List of snapshots ordered by snapshot number.</returns>
    Task<IReadOnlyList<WorkflowSnapshot>> GetSnapshotsAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest snapshot for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The latest snapshot if found; otherwise, null.</returns>
    Task<WorkflowSnapshot?> GetLatestSnapshotAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a milestone.
    /// </summary>
    /// <param name="milestone">The milestone to create.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The created milestone.</returns>
    Task<Milestone> CreateMilestoneAsync(Milestone milestone, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a milestone.
    /// </summary>
    /// <param name="milestone">The milestone with updated values.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The updated milestone.</returns>
    Task<Milestone> UpdateMilestoneAsync(Milestone milestone, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all milestones for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>List of milestones ordered by order property.</returns>
    Task<IReadOnlyList<Milestone>> GetMilestonesAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates session context.
    /// </summary>
    /// <param name="context">The session context to create or update.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The created or updated context.</returns>
    Task<SessionContext> CreateOrUpdateContextAsync(SessionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets session context by session identifier.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The session context if found; otherwise, null.</returns>
    Task<SessionContext?> GetContextAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a bootstrap payload for session resumption.
    /// </summary>
    /// <param name="data">The bootstrap data.</param>
    /// <param name="format">The output format.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The generated bootstrap payload.</returns>
    Task<BootstrapPayload> GenerateBootstrapAsync(
        BootstrapData data,
        BootstrapFormat format = BootstrapFormat.Json,
        CancellationToken cancellationToken = default);
}
