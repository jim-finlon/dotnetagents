namespace DotNetAgents.Agents.Tasks;

/// <summary>
/// Store for task state and results.
/// </summary>
public interface ITaskStore
{
    /// <summary>
    /// Saves a task to the store.
    /// </summary>
    /// <param name="task">The task to save.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveAsync(
        WorkerTask task,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a task by ID.
    /// </summary>
    /// <param name="taskId">The ID of the task.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The task, or null if not found.</returns>
    Task<WorkerTask?> GetAsync(
        string taskId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a task result to the store.
    /// </summary>
    /// <param name="result">The task result to save.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveResultAsync(
        WorkerTaskResult result,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a task result by task ID.
    /// </summary>
    /// <param name="taskId">The ID of the task.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The task result, or null if not found.</returns>
    Task<WorkerTaskResult?> GetResultAsync(
        string taskId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of a task.
    /// </summary>
    /// <param name="taskId">The ID of the task.</param>
    /// <param name="status">The new status.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateStatusAsync(
        string taskId,
        TaskStatus status,
        CancellationToken cancellationToken = default);
}
