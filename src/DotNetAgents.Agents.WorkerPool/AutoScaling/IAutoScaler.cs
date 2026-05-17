using DotNetAgents.Agents.Registry;

namespace DotNetAgents.Agents.WorkerPool.AutoScaling;

/// <summary>
/// Interface for auto-scaling worker pools based on load metrics.
/// </summary>
public interface IAutoScaler
{
    /// <summary>
    /// Evaluates whether the worker pool should be scaled up or down based on current metrics.
    /// </summary>
    /// <param name="currentWorkers">Current list of workers in the pool.</param>
    /// <param name="pendingTaskCount">Number of pending tasks.</param>
    /// <param name="averageTaskDuration">Average task duration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Scaling decision indicating whether to scale up, down, or maintain current size.</returns>
    Task<ScalingDecision> EvaluateScalingAsync(
        IReadOnlyList<AgentInfo> currentWorkers,
        int pendingTaskCount,
        TimeSpan averageTaskDuration,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a decision to scale the worker pool.
/// </summary>
public record ScalingDecision
{
    /// <summary>
    /// Gets the scaling action to take.
    /// </summary>
    public ScalingAction Action { get; init; }

    /// <summary>
    /// Gets the number of workers to add or remove (absolute value).
    /// </summary>
    public int WorkerCount { get; init; }

    /// <summary>
    /// Gets the reason for the scaling decision.
    /// </summary>
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Represents the type of scaling action.
/// </summary>
public enum ScalingAction
{
    /// <summary>
    /// No scaling action needed.
    /// </summary>
    None,

    /// <summary>
    /// Scale up (add workers).
    /// </summary>
    ScaleUp,

    /// <summary>
    /// Scale down (remove workers).
    /// </summary>
    ScaleDown
}
