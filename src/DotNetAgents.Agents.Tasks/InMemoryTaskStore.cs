using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.Tasks;

/// <summary>
/// In-memory implementation of <see cref="ITaskStore"/>.
/// Suitable for single-instance deployments.
/// </summary>
public class InMemoryTaskStore : ITaskStore
{
    private readonly ILogger<InMemoryTaskStore>? _logger;
    private readonly Dictionary<string, WorkerTask> _tasks = new();
    private readonly Dictionary<string, WorkerTaskResult> _results = new();
    private readonly Dictionary<string, TaskStatus> _statuses = new();
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryTaskStore"/> class.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    public InMemoryTaskStore(ILogger<InMemoryTaskStore>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task SaveAsync(
        WorkerTask task,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _tasks[task.TaskId] = task;
            _statuses[task.TaskId] = TaskStatus.Pending;

            _logger?.LogDebug("Saved task {TaskId} to store", task.TaskId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<WorkerTask?> GetAsync(
        string taskId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(taskId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _tasks.TryGetValue(taskId, out var task);
            return Task.FromResult(task);
        }
    }

    /// <inheritdoc />
    public Task SaveResultAsync(
        WorkerTaskResult result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _results[result.TaskId] = result;
            _statuses[result.TaskId] = result.Success ? TaskStatus.Completed : TaskStatus.Failed;

            _logger?.LogDebug(
                "Saved result for task {TaskId} (Success: {Success})",
                result.TaskId,
                result.Success);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<WorkerTaskResult?> GetResultAsync(
        string taskId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(taskId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _results.TryGetValue(taskId, out var result);
            return Task.FromResult(result);
        }
    }

    /// <inheritdoc />
    public Task UpdateStatusAsync(
        string taskId,
        TaskStatus status,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(taskId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _statuses[taskId] = status;
            _logger?.LogDebug("Updated status for task {TaskId} to {Status}", taskId, status);
        }

        return Task.CompletedTask;
    }
}
