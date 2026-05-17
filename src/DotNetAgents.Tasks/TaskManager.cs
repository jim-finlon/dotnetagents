using DotNetAgents.Abstractions.Exceptions;
using DotNetAgents.Tasks.Models;
using DotNetAgents.Tasks.Storage;
using Microsoft.Extensions.Logging;
using TaskStatus = DotNetAgents.Tasks.Models.TaskStatus;

namespace DotNetAgents.Tasks;

/// <summary>
/// Default implementation of <see cref="ITaskManager"/>.
/// </summary>
public class TaskManager : ITaskManager
{
    private readonly ITaskStore _taskStore;
    private readonly ILogger<TaskManager> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskManager"/> class.
    /// </summary>
    /// <param name="taskStore">The task store.</param>
    /// <param name="logger">The logger.</param>
    public TaskManager(ITaskStore taskStore, ILogger<TaskManager> logger)
    {
        _taskStore = taskStore ?? throw new ArgumentNullException(nameof(taskStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<WorkTask> CreateTaskAsync(WorkTask task, CancellationToken cancellationToken = default)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));

        if (string.IsNullOrWhiteSpace(task.SessionId))
            throw new ArgumentException("Session ID cannot be null or whitespace.", nameof(task));

        if (string.IsNullOrWhiteSpace(task.Content))
            throw new ArgumentException("Task content cannot be null or whitespace.", nameof(task));

        try
        {
            _logger.LogDebug("Creating task for session {SessionId}", task.SessionId);

            var createdTask = await _taskStore.CreateAsync(task, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Task created. TaskId: {TaskId}, SessionId: {SessionId}, Status: {Status}",
                createdTask.Id,
                createdTask.SessionId,
                createdTask.Status);

            return createdTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create task for session {SessionId}", task.SessionId);
            throw new AgentException(
                $"Failed to create task: {ex.Message}",
                ErrorCategory.ConfigurationError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<WorkTask?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        if (taskId == default)
            throw new ArgumentException("Task ID cannot be default.", nameof(taskId));

        try
        {
            _logger.LogDebug("Getting task {TaskId}", taskId);

            var task = await _taskStore.GetByIdAsync(taskId, cancellationToken).ConfigureAwait(false);

            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found", taskId);
            }

            return task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get task {TaskId}", taskId);
            throw new AgentException(
                $"Failed to get task: {ex.Message}",
                ErrorCategory.ConfigurationError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<WorkTask> UpdateTaskAsync(WorkTask task, CancellationToken cancellationToken = default)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));

        if (task.Id == default)
            throw new ArgumentException("Task ID cannot be default.", nameof(task));

        try
        {
            _logger.LogDebug("Updating task {TaskId}", task.Id);

            // Verify task exists
            var existingTask = await _taskStore.GetByIdAsync(task.Id, cancellationToken).ConfigureAwait(false);
            if (existingTask == null)
            {
                throw new InvalidOperationException($"Task {task.Id} not found.");
            }

            var updatedTask = await _taskStore.UpdateAsync(task, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Task updated. TaskId: {TaskId}, Status: {Status}",
                updatedTask.Id,
                updatedTask.Status);

            return updatedTask;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update task {TaskId}", task.Id);
            throw new AgentException(
                $"Failed to update task: {ex.Message}",
                ErrorCategory.ConfigurationError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        if (taskId == default)
            throw new ArgumentException("Task ID cannot be default.", nameof(taskId));

        try
        {
            _logger.LogDebug("Deleting task {TaskId}", taskId);

            await _taskStore.DeleteAsync(taskId, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Task deleted. TaskId: {TaskId}", taskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete task {TaskId}", taskId);
            throw new AgentException(
                $"Failed to delete task: {ex.Message}",
                ErrorCategory.ConfigurationError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<WorkTask>> GetTasksBySessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or whitespace.", nameof(sessionId));

        try
        {
            _logger.LogDebug("Getting tasks for session {SessionId}", sessionId);

            var tasks = await _taskStore.GetBySessionIdAsync(sessionId, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Retrieved {Count} tasks for session {SessionId}", tasks.Count, sessionId);

            return tasks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tasks for session {SessionId}", sessionId);
            throw new AgentException(
                $"Failed to get tasks: {ex.Message}",
                ErrorCategory.ConfigurationError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<WorkTask>> GetTasksByStatusAsync(
        string sessionId,
        TaskStatus status,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or whitespace.", nameof(sessionId));

        try
        {
            _logger.LogDebug("Getting tasks with status {Status} for session {SessionId}", status, sessionId);

            var tasks = await _taskStore.GetByStatusAsync(sessionId, status, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Retrieved {Count} tasks with status {Status} for session {SessionId}",
                tasks.Count,
                status,
                sessionId);

            return tasks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tasks by status for session {SessionId}", sessionId);
            throw new AgentException(
                $"Failed to get tasks by status: {ex.Message}",
                ErrorCategory.ConfigurationError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<WorkTask>> GetTasksByWorkflowRunAsync(
        string workflowRunId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workflowRunId))
            throw new ArgumentException("Workflow run ID cannot be null or whitespace.", nameof(workflowRunId));

        try
        {
            _logger.LogDebug("Getting tasks for workflow run {WorkflowRunId}", workflowRunId);

            var tasks = await _taskStore.GetByWorkflowRunIdAsync(workflowRunId, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Retrieved {Count} tasks for workflow run {WorkflowRunId}",
                tasks.Count,
                workflowRunId);

            return tasks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tasks for workflow run {WorkflowRunId}", workflowRunId);
            throw new AgentException(
                $"Failed to get tasks for workflow run: {ex.Message}",
                ErrorCategory.ConfigurationError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<TaskStatistics> GetTaskStatisticsAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or whitespace.", nameof(sessionId));

        try
        {
            _logger.LogDebug("Getting task statistics for session {SessionId}", sessionId);

            var stats = await _taskStore.GetStatisticsAsync(sessionId, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Task statistics for session {SessionId}: Total={Total}, Completed={Completed}, Completion={CompletionPercentage:F1}%",
                sessionId,
                stats.Total,
                stats.Completed,
                stats.CompletionPercentage);

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get task statistics for session {SessionId}", sessionId);
            throw new AgentException(
                $"Failed to get task statistics: {ex.Message}",
                ErrorCategory.ConfigurationError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task ReorderTasksAsync(
        string sessionId,
        Dictionary<Guid, int> taskOrders,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or whitespace.", nameof(sessionId));

        if (taskOrders == null)
            throw new ArgumentNullException(nameof(taskOrders));

        try
        {
            _logger.LogDebug("Reordering {Count} tasks for session {SessionId}", taskOrders.Count, sessionId);

            await _taskStore.ReorderAsync(sessionId, taskOrders, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Tasks reordered for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reorder tasks for session {SessionId}", sessionId);
            throw new AgentException(
                $"Failed to reorder tasks: {ex.Message}",
                ErrorCategory.ConfigurationError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> AreDependenciesCompleteAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        if (taskId == default)
            throw new ArgumentException("Task ID cannot be default.", nameof(taskId));

        try
        {
            _logger.LogDebug("Checking dependencies for task {TaskId}", taskId);

            var areComplete = await _taskStore.AreDependenciesCompleteAsync(taskId, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Dependencies for task {TaskId} are {Status}",
                taskId,
                areComplete ? "complete" : "incomplete");

            return areComplete;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check dependencies for task {TaskId}", taskId);
            throw new AgentException(
                $"Failed to check dependencies: {ex.Message}",
                ErrorCategory.ConfigurationError,
                ex);
        }
    }
}
