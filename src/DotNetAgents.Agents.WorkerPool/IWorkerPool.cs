using DotNetAgents.Agents.Registry;
using DotNetAgents.Agents.WorkerPool.LoadBalancing;

namespace DotNetAgents.Agents.WorkerPool;

/// <summary>
/// Manages a pool of worker agents.
/// </summary>
public interface IWorkerPool
{
    /// <summary>
    /// Gets the current number of workers in the pool.
    /// </summary>
    int WorkerCount { get; }

    /// <summary>
    /// Gets the number of available workers.
    /// </summary>
    int AvailableWorkerCount { get; }

    /// <summary>
    /// Adds a worker to the pool.
    /// </summary>
    /// <param name="agentId">The ID of the agent to add.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddWorkerAsync(
        string agentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a worker from the pool.
    /// </summary>
    /// <param name="agentId">The ID of the agent to remove.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveWorkerAsync(
        string agentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an available worker for a task.
    /// </summary>
    /// <param name="requiredCapability">Optional required capability for the task.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>An available worker agent, or null if none available.</returns>
    Task<AgentInfo?> GetAvailableWorkerAsync(
        string? requiredCapability = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pool statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Statistics about the worker pool.</returns>
    Task<WorkerPoolStatistics> GetStatisticsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates auto-scaling and returns a scaling decision.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A scaling decision indicating whether to scale up, down, or maintain.</returns>
    Task<AutoScaling.ScalingDecision> EvaluateAutoScalingAsync(
        CancellationToken cancellationToken = default);
}
