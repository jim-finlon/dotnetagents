using DotNetAgents.Tasks.Models;
using TaskStatus = DotNetAgents.Tasks.Models.TaskStatus;

namespace DotNetAgents.Tasks;

/// <summary>
/// Interface for managing work tasks.
/// </summary>
public interface ITaskManager
{
    /// <summary>
    /// Creates a new task.
    /// </summary>
    /// <param name="task">The task to create.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The created task.</returns>
    Task<WorkTask> CreateTaskAsync(WorkTask task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a task by its unique identifier.
    /// </summary>
    /// <param name="taskId">The task identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The task if found; otherwise, null.</returns>
    Task<WorkTask?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing task.
    /// </summary>
    /// <param name="task">The task with updated values.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The updated task.</returns>
    Task<WorkTask> UpdateTaskAsync(WorkTask task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a task.
    /// </summary>
    /// <param name="taskId">The task identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tasks for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>List of tasks for the session.</returns>
    Task<IReadOnlyList<WorkTask>> GetTasksBySessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tasks by status for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="status">The task status filter.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>Filtered list of tasks.</returns>
    Task<IReadOnlyList<WorkTask>> GetTasksByStatusAsync(
        string sessionId,
        TaskStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tasks by workflow run identifier.
    /// </summary>
    /// <param name="workflowRunId">The workflow run identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>List of tasks for the workflow run.</returns>
    Task<IReadOnlyList<WorkTask>> GetTasksByWorkflowRunAsync(
        string workflowRunId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets task statistics for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>Task statistics.</returns>
    Task<TaskStatistics> GetTaskStatisticsAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reorders tasks within a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="taskOrders">Dictionary of TaskId to new Order value.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReorderTasksAsync(
        string sessionId,
        Dictionary<Guid, int> taskOrders,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if all task dependencies are completed.
    /// </summary>
    /// <param name="taskId">The task identifier to check.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>True if all dependencies are completed; otherwise, false.</returns>
    Task<bool> AreDependenciesCompleteAsync(
        Guid taskId,
        CancellationToken cancellationToken = default);
}
