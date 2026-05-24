// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Agents.Tasks;

/// <summary>
/// Queue for managing worker tasks.
/// </summary>
public interface ITaskQueue
{
    /// <summary>
    /// Enqueues a task for execution.
    /// </summary>
    /// <param name="task">The task to enqueue.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EnqueueAsync(
        WorkerTask task,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dequeues the next available task.
    /// </summary>
    /// <param name="agentId">Optional agent ID to filter tasks for a specific agent.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The next available task, or null if the queue is empty.</returns>
    Task<WorkerTask?> DequeueAsync(
        string? agentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the number of pending tasks.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The number of pending tasks.</returns>
    Task<int> GetPendingCountAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a payload-free posture snapshot for control-loop dashboards and operator signals.
    /// </summary>
    /// <param name="queueKey">Optional queue key used to label the projection.</param>
    /// <param name="maxDepthBudget">Optional depth budget for backpressure classification.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The current queue posture snapshot.</returns>
    Task<QueuePostureSnapshot> GetPostureAsync(
        string? queueKey = null,
        int? maxDepthBudget = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Peek at the next task without dequeuing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The next task in the queue, or null if the queue is empty.</returns>
    Task<WorkerTask?> PeekAsync(
        CancellationToken cancellationToken = default);
}
