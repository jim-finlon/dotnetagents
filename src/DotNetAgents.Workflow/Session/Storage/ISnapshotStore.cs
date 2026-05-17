using DotNetAgents.Workflow.Session;

namespace DotNetAgents.Workflow.Session.Storage;

/// <summary>
/// Interface for storing and retrieving workflow snapshots.
/// </summary>
public interface ISnapshotStore
{
    /// <summary>
    /// Creates a new snapshot.
    /// </summary>
    /// <param name="snapshot">The snapshot to create.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The created snapshot.</returns>
    Task<WorkflowSnapshot> CreateAsync(WorkflowSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a snapshot by its unique identifier.
    /// </summary>
    /// <param name="snapshotId">The snapshot identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The snapshot if found; otherwise, null.</returns>
    Task<WorkflowSnapshot?> GetByIdAsync(Guid snapshotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all snapshots for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>List of snapshots ordered by snapshot number.</returns>
    Task<IReadOnlyList<WorkflowSnapshot>> GetBySessionIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest snapshot for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The latest snapshot if found; otherwise, null.</returns>
    Task<WorkflowSnapshot?> GetLatestAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a snapshot.
    /// </summary>
    /// <param name="snapshotId">The snapshot identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(Guid snapshotId, CancellationToken cancellationToken = default);
}
