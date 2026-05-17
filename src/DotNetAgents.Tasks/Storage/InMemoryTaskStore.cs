using DotNetAgents.Tasks.Models;
using TaskStatus = DotNetAgents.Tasks.Models.TaskStatus;

namespace DotNetAgents.Tasks.Storage;

/// <summary>
/// In-memory implementation of <see cref="ITaskStore"/> for testing and development.
/// </summary>
public class InMemoryTaskStore : ITaskStore
{
    private readonly Dictionary<Guid, WorkTask> _tasks = new();
    private readonly Dictionary<string, List<Guid>> _sessionTasks = new();
    private readonly Dictionary<string, List<Guid>> _workflowRunTasks = new();
    private readonly object _lock = new();

    /// <inheritdoc/>
    public Task<WorkTask?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _tasks.TryGetValue(taskId, out var task);
            return Task.FromResult<WorkTask?>(task);
        }
    }

    /// <inheritdoc/>
    public Task<WorkTask> CreateAsync(WorkTask task, CancellationToken cancellationToken = default)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));

        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            // Auto-assign order if not set
            var order = task.Order;
            if (order == 0)
            {
                var existingTasks = GetTasksForSession(task.SessionId);
                order = existingTasks.Any() ? existingTasks.Max(t => t.Order) + 1 : 0;
            }

            var taskToCreate = task with
            {
                Id = task.Id == default ? Guid.NewGuid() : task.Id,
                CreatedAt = task.CreatedAt == default ? DateTimeOffset.UtcNow : task.CreatedAt,
                UpdatedAt = DateTimeOffset.UtcNow,
                Order = order
            };

            _tasks[taskToCreate.Id] = taskToCreate;

            // Track by session
            if (!_sessionTasks.ContainsKey(taskToCreate.SessionId))
            {
                _sessionTasks[taskToCreate.SessionId] = new List<Guid>();
            }
            _sessionTasks[taskToCreate.SessionId].Add(taskToCreate.Id);

            // Track by workflow run
            if (!string.IsNullOrWhiteSpace(taskToCreate.WorkflowRunId))
            {
                if (!_workflowRunTasks.ContainsKey(taskToCreate.WorkflowRunId))
                {
                    _workflowRunTasks[taskToCreate.WorkflowRunId] = new List<Guid>();
                }
                _workflowRunTasks[taskToCreate.WorkflowRunId].Add(taskToCreate.Id);
            }

            return Task.FromResult(taskToCreate);
        }
    }

    /// <inheritdoc/>
    public Task<WorkTask> UpdateAsync(WorkTask task, CancellationToken cancellationToken = default)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));

        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (!_tasks.ContainsKey(task.Id))
            {
                throw new InvalidOperationException($"Task {task.Id} not found.");
            }

            // Set timestamps based on status changes
            var existingTask = _tasks[task.Id];
            var now = DateTimeOffset.UtcNow;

            var updatedTask = task with
            {
                UpdatedAt = now,
                StartedAt = GetStartedAt(existingTask, task, now),
                CompletedAt = GetCompletedAt(existingTask, task, now),
                CancelledAt = GetCancelledAt(existingTask, task, now)
            };

            _tasks[task.Id] = updatedTask;
            return Task.FromResult(updatedTask);
        }
    }

    /// <inheritdoc/>
    public Task DeleteAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (!_tasks.TryGetValue(taskId, out var task))
            {
                return Task.CompletedTask;
            }

            _tasks.Remove(taskId);

            // Remove from session tracking
            if (_sessionTasks.TryGetValue(task.SessionId, out var sessionTaskIds))
            {
                sessionTaskIds.Remove(taskId);
                if (sessionTaskIds.Count == 0)
                {
                    _sessionTasks.Remove(task.SessionId);
                }
            }

            // Remove from workflow run tracking
            if (!string.IsNullOrWhiteSpace(task.WorkflowRunId) &&
                _workflowRunTasks.TryGetValue(task.WorkflowRunId, out var workflowTaskIds))
            {
                workflowTaskIds.Remove(taskId);
                if (workflowTaskIds.Count == 0)
                {
                    _workflowRunTasks.Remove(task.WorkflowRunId);
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<WorkTask>> GetBySessionIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or whitespace.", nameof(sessionId));

        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var tasks = GetTasksForSession(sessionId)
                .OrderBy(t => t.Order)
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<WorkTask>>(tasks);
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<WorkTask>> GetByStatusAsync(
        string sessionId,
        TaskStatus status,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or whitespace.", nameof(sessionId));

        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var tasks = GetTasksForSession(sessionId)
                .Where(t => t.Status == status)
                .OrderBy(t => t.Order)
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<WorkTask>>(tasks);
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<WorkTask>> GetByWorkflowRunIdAsync(
        string workflowRunId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workflowRunId))
            throw new ArgumentException("Workflow run ID cannot be null or whitespace.", nameof(workflowRunId));

        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (!_workflowRunTasks.TryGetValue(workflowRunId, out var taskIds))
            {
                return Task.FromResult<IReadOnlyList<WorkTask>>(Array.Empty<WorkTask>());
            }

            var tasks = taskIds
                .Select(id => _tasks.TryGetValue(id, out var task) ? task : null)
                .Where(t => t != null)
                .Cast<WorkTask>()
                .OrderBy(t => t.Order)
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<WorkTask>>(tasks);
        }
    }

    /// <inheritdoc/>
    public Task<TaskStatistics> GetStatisticsAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or whitespace.", nameof(sessionId));

        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var tasks = GetTasksForSession(sessionId).ToList();

            var stats = new TaskStatistics
            {
                Total = tasks.Count,
                Pending = tasks.Count(t => t.Status == TaskStatus.Pending),
                InProgress = tasks.Count(t => t.Status == TaskStatus.InProgress),
                Completed = tasks.Count(t => t.Status == TaskStatus.Completed),
                Blocked = tasks.Count(t => t.Status == TaskStatus.Blocked),
                Cancelled = tasks.Count(t => t.Status == TaskStatus.Cancelled),
                Review = tasks.Count(t => t.Status == TaskStatus.Review)
            };

            return Task.FromResult(stats);
        }
    }

    /// <inheritdoc/>
    public Task ReorderAsync(
        string sessionId,
        Dictionary<Guid, int> taskOrders,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or whitespace.", nameof(sessionId));

        if (taskOrders == null)
            throw new ArgumentNullException(nameof(taskOrders));

        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var (taskId, newOrder) in taskOrders)
            {
                if (_tasks.TryGetValue(taskId, out var task) && task.SessionId == sessionId)
                {
                    _tasks[taskId] = task with
                    {
                        Order = newOrder,
                        UpdatedAt = now
                    };
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> AreDependenciesCompleteAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (!_tasks.TryGetValue(taskId, out var task))
            {
                return Task.FromResult(false);
            }

            // If no dependencies, return true
            if (task.DependsOn == null || task.DependsOn.Count == 0)
            {
                return Task.FromResult(true);
            }

            // Check if all dependency tasks are completed
            foreach (var depId in task.DependsOn)
            {
                if (!_tasks.TryGetValue(depId, out var depTask) ||
                    depTask.Status != TaskStatus.Completed)
                {
                    return Task.FromResult(false);
                }
            }

            return Task.FromResult(true);
        }
    }

    private List<WorkTask> GetTasksForSession(string sessionId)
    {
        if (!_sessionTasks.TryGetValue(sessionId, out var taskIds))
        {
            return new List<WorkTask>();
        }

        return taskIds
            .Select(id => _tasks.TryGetValue(id, out var task) ? task : null)
            .Where(t => t != null)
            .Cast<WorkTask>()
            .ToList();
    }

    private static DateTimeOffset? GetStartedAt(WorkTask existing, WorkTask updated, DateTimeOffset now)
    {
        if (updated.Status == TaskStatus.InProgress && existing.Status != TaskStatus.InProgress)
        {
            return updated.StartedAt ?? now;
        }
        return updated.StartedAt ?? existing.StartedAt;
    }

    private static DateTimeOffset? GetCompletedAt(WorkTask existing, WorkTask updated, DateTimeOffset now)
    {
        if (updated.Status == TaskStatus.Completed && existing.Status != TaskStatus.Completed)
        {
            return updated.CompletedAt ?? now;
        }
        return updated.CompletedAt ?? existing.CompletedAt;
    }

    private static DateTimeOffset? GetCancelledAt(WorkTask existing, WorkTask updated, DateTimeOffset now)
    {
        if (updated.Status == TaskStatus.Cancelled && existing.Status != TaskStatus.Cancelled)
        {
            return updated.CancelledAt ?? now;
        }
        return updated.CancelledAt ?? existing.CancelledAt;
    }
}
