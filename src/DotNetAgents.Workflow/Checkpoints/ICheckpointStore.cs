namespace DotNetAgents.Workflow.Checkpoints;

/// <summary>
/// Interface for storing and retrieving workflow checkpoints.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public interface ICheckpointStore<TState> where TState : class
{
    /// <summary>
    /// Saves a checkpoint.
    /// </summary>
    /// <param name="checkpoint">The checkpoint to save.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The ID of the saved checkpoint.</returns>
    Task<string> SaveAsync(
        Checkpoint<TState> checkpoint,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a checkpoint by ID.
    /// </summary>
    /// <param name="checkpointId">The ID of the checkpoint to retrieve.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The checkpoint if found; otherwise, null.</returns>
    Task<Checkpoint<TState>?> GetAsync(
        string checkpointId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest checkpoint for a workflow run.
    /// </summary>
    /// <param name="runId">The workflow run identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The latest checkpoint if found; otherwise, null.</returns>
    Task<Checkpoint<TState>?> GetLatestAsync(
        string runId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all checkpoints for a workflow run.
    /// </summary>
    /// <param name="runId">The workflow run identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A list of checkpoints for the run, ordered by creation time.</returns>
    Task<IReadOnlyList<Checkpoint<TState>>> ListAsync(
        string runId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a checkpoint.
    /// </summary>
    /// <param name="checkpointId">The ID of the checkpoint to delete.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteAsync(
        string checkpointId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all checkpoints older than the specified time.
    /// </summary>
    /// <param name="olderThan">The cutoff time. Checkpoints older than this will be deleted.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The number of checkpoints deleted.</returns>
    Task<int> DeleteOlderThanAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default);
}
