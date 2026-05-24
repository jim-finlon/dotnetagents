// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.Tasks;

/// <summary>
/// In-memory implementation of <see cref="ITaskQueue"/>.
/// Suitable for single-instance deployments.
/// </summary>
public class InMemoryTaskQueue : ITaskQueue
{
    private readonly ILogger<InMemoryTaskQueue>? _logger;
    private readonly SortedSet<QueuedTask> _queue = new(new TaskPriorityComparer());
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryTaskQueue"/> class.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    public InMemoryTaskQueue(ILogger<InMemoryTaskQueue>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task EnqueueAsync(
        WorkerTask task,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var queuedTask = new QueuedTask(task, DateTimeOffset.UtcNow);
            _queue.Add(queuedTask);

            _logger?.LogDebug(
                "Enqueued task {TaskId} of type {TaskType} with priority {Priority}",
                task.TaskId,
                task.TaskType,
                task.Priority);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<WorkerTask?> DequeueAsync(
        string? agentId = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (_queue.Count == 0)
            {
                return Task.FromResult<WorkerTask?>(null);
            }

            QueuedTask? queuedTask = null;

            if (agentId != null)
            {
                // SortedSet order: Min = highest priority, then oldest enqueue (see TaskPriorityComparer).
                // Match tasks assigned to this agent or unassigned (any worker may take them).
                foreach (var qt in _queue)
                {
                    if (qt.Task.PreferredAgentId == agentId ||
                        string.IsNullOrEmpty(qt.Task.PreferredAgentId))
                    {
                        queuedTask = qt;
                        break;
                    }
                }
            }

            // If no agent filter (or no matching preferred/unassigned task), take next global task.
            queuedTask ??= _queue.Min;

            if (queuedTask != null)
            {
                _queue.Remove(queuedTask);
                _logger?.LogDebug(
                    "Dequeued task {TaskId} of type {TaskType}",
                    queuedTask.Task.TaskId,
                    queuedTask.Task.TaskType);
                return Task.FromResult<WorkerTask?>(queuedTask.Task);
            }

            return Task.FromResult<WorkerTask?>(null);
        }
    }

    /// <inheritdoc />
    public Task<int> GetPendingCountAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            return Task.FromResult(_queue.Count);
        }
    }

    /// <inheritdoc />
    public Task<QueuePostureSnapshot> GetPostureAsync(
        string? queueKey = null,
        int? maxDepthBudget = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var items = _queue
                .Select(static queued => ToSnapshot(queued))
                .ToArray();
            var pendingDepth = items.Length;
            var oldestPending = items
                .Where(static item => item.Disposition == QueueWorkDisposition.Pending)
                .Select(static item => item.EnqueuedAtUtc)
                .Where(static item => item.HasValue)
                .OrderBy(static item => item)
                .FirstOrDefault();

            return Task.FromResult(new QueuePostureSnapshot(
                queueKey ?? "default",
                ClassifyBackpressure(pendingDepth, maxDepthBudget),
                pendingDepth,
                ActiveCount: 0,
                CompletedCount: 0,
                FailedCount: 0,
                RetryingCount: items.Count(static item => item.RetryBudget.Attempts > 0),
                MaxDepthBudget: maxDepthBudget,
                OldestPendingUtc: oldestPending,
                WorkItems: items));
        }
    }

    /// <inheritdoc />
    public Task<WorkerTask?> PeekAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (_queue.Count == 0)
            {
                return Task.FromResult<WorkerTask?>(null);
            }

            var queuedTask = _queue.Min;
            return Task.FromResult<WorkerTask?>(queuedTask?.Task);
        }
    }

    private static QueueWorkItemSnapshot ToSnapshot(QueuedTask queued)
    {
        var task = queued.Task;
        return new QueueWorkItemSnapshot(
            new QueueWorkItemIdentity(
                task.TaskId,
                task.TaskType,
                task.QueueKey,
                task.CorrelationId,
                task.StoryId,
                task.ParentTaskId),
            QueueWorkDisposition.Pending,
            new RetryBudget(task.RetryCount, task.MaxRetryAttempts, task.NextRetryAtUtc),
            task.Priority,
            task.CreatedAt,
            queued.EnqueuedAt);
    }

    private static QueueBackpressureState ClassifyBackpressure(int pendingDepth, int? maxDepthBudget)
    {
        if (pendingDepth == 0)
            return QueueBackpressureState.Empty;
        if (maxDepthBudget is null or <= 0)
            return QueueBackpressureState.Normal;
        if (pendingDepth >= maxDepthBudget)
            return QueueBackpressureState.Saturated;
        if (pendingDepth >= Math.Ceiling(maxDepthBudget.Value * 0.8d))
            return QueueBackpressureState.Constrained;
        return QueueBackpressureState.Normal;
    }

    private sealed class QueuedTask
    {
        public WorkerTask Task { get; }
        public DateTimeOffset EnqueuedAt { get; }

        public QueuedTask(WorkerTask task, DateTimeOffset enqueuedAt)
        {
            Task = task;
            EnqueuedAt = enqueuedAt;
        }
    }

    private sealed class TaskPriorityComparer : IComparer<QueuedTask>
    {
        public int Compare(QueuedTask? x, QueuedTask? y)
        {
            if (x == null && y == null)
                return 0;
            if (x == null)
                return -1;
            if (y == null)
                return 1;

            // Compare by priority (higher priority first)
            var priorityComparison = y.Task.Priority.CompareTo(x.Task.Priority);
            if (priorityComparison != 0)
                return priorityComparison;

            // If priorities are equal, compare by creation time (older first)
            return x.EnqueuedAt.CompareTo(y.EnqueuedAt);
        }
    }
}
