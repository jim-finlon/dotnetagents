using DotNetAgents.Agents.Registry;

namespace DotNetAgents.Agents.WorkerPool.AutoScaling;

/// <summary>
/// Default implementation of <see cref="IAutoScaler"/> with configurable thresholds.
/// </summary>
public class AutoScaler : IAutoScaler
{
    private readonly AutoScalingOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoScaler"/> class.
    /// </summary>
    /// <param name="options">Auto-scaling configuration options.</param>
    public AutoScaler(AutoScalingOptions? options = null)
    {
        _options = options ?? new AutoScalingOptions();
    }

    /// <inheritdoc />
    public Task<ScalingDecision> EvaluateScalingAsync(
        IReadOnlyList<AgentInfo> currentWorkers,
        int pendingTaskCount,
        TimeSpan averageTaskDuration,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var availableWorkers = currentWorkers.Count(w =>
            w.Status == AgentStatus.Available &&
            w.CurrentTaskCount < w.Capabilities.MaxConcurrentTasks);

        var totalCapacity = currentWorkers.Sum(w => w.Capabilities.MaxConcurrentTasks);
        var currentLoad = currentWorkers.Sum(w => w.CurrentTaskCount);
        var utilizationRate = totalCapacity > 0 ? (double)currentLoad / totalCapacity : 0.0;

        // Calculate tasks per available worker
        var tasksPerWorker = availableWorkers > 0
            ? (double)pendingTaskCount / availableWorkers
            : pendingTaskCount;

        // Scale up conditions
        if (pendingTaskCount > _options.ScaleUpTaskThreshold &&
            tasksPerWorker > _options.ScaleUpTasksPerWorkerThreshold &&
            utilizationRate > _options.ScaleUpUtilizationThreshold &&
            currentWorkers.Count < _options.MaxWorkers)
        {
            var workersToAdd = CalculateWorkersToAdd(
                pendingTaskCount,
                availableWorkers,
                _options.ScaleUpTasksPerWorkerThreshold);

            return Task.FromResult(new ScalingDecision
            {
                Action = ScalingAction.ScaleUp,
                WorkerCount = Math.Min(workersToAdd, _options.MaxWorkers - currentWorkers.Count),
                Reason = $"High load: {pendingTaskCount} pending tasks, {utilizationRate:P0} utilization, {tasksPerWorker:F1} tasks/worker"
            });
        }

        // Scale down conditions
        if (pendingTaskCount < _options.ScaleDownTaskThreshold &&
            utilizationRate < _options.ScaleDownUtilizationThreshold &&
            currentWorkers.Count > _options.MinWorkers &&
            averageTaskDuration < _options.ScaleDownMaxTaskDuration)
        {
            var workersToRemove = CalculateWorkersToRemove(
                pendingTaskCount,
                currentWorkers.Count,
                _options.ScaleDownTasksPerWorkerThreshold);

            return Task.FromResult(new ScalingDecision
            {
                Action = ScalingAction.ScaleDown,
                WorkerCount = Math.Min(workersToRemove, currentWorkers.Count - _options.MinWorkers),
                Reason = $"Low load: {pendingTaskCount} pending tasks, {utilizationRate:P0} utilization"
            });
        }

        return Task.FromResult(new ScalingDecision
        {
            Action = ScalingAction.None,
            WorkerCount = 0,
            Reason = "Load within acceptable range"
        });
    }

    private int CalculateWorkersToAdd(int pendingTasks, int availableWorkers, double targetTasksPerWorker)
    {
        var targetWorkers = (int)Math.Ceiling(pendingTasks / targetTasksPerWorker);
        var workersNeeded = Math.Max(0, targetWorkers - availableWorkers);
        return Math.Min(workersNeeded, _options.ScaleUpIncrement);
    }

    private int CalculateWorkersToRemove(int pendingTasks, int currentWorkers, double targetTasksPerWorker)
    {
        var targetWorkers = (int)Math.Ceiling(pendingTasks / targetTasksPerWorker);
        var excessWorkers = Math.Max(0, currentWorkers - targetWorkers);
        return Math.Min(excessWorkers, _options.ScaleDownIncrement);
    }
}

/// <summary>
/// Configuration options for auto-scaling.
/// </summary>
public class AutoScalingOptions
{
    /// <summary>
    /// Gets or sets the minimum number of workers to maintain.
    /// </summary>
    public int MinWorkers { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum number of workers allowed.
    /// </summary>
    public int MaxWorkers { get; set; } = 100;

    /// <summary>
    /// Gets or sets the threshold for pending tasks to trigger scale-up.
    /// </summary>
    public int ScaleUpTaskThreshold { get; set; } = 10;

    /// <summary>
    /// Gets or sets the threshold for tasks per worker to trigger scale-up.
    /// </summary>
    public double ScaleUpTasksPerWorkerThreshold { get; set; } = 5.0;

    /// <summary>
    /// Gets or sets the utilization threshold (0.0-1.0) to trigger scale-up.
    /// </summary>
    public double ScaleUpUtilizationThreshold { get; set; } = 0.75;

    /// <summary>
    /// Gets or sets the number of workers to add when scaling up.
    /// </summary>
    public int ScaleUpIncrement { get; set; } = 2;

    /// <summary>
    /// Gets or sets the threshold for pending tasks to trigger scale-down.
    /// </summary>
    public int ScaleDownTaskThreshold { get; set; } = 2;

    /// <summary>
    /// Gets or sets the threshold for tasks per worker to trigger scale-down.
    /// </summary>
    public double ScaleDownTasksPerWorkerThreshold { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the utilization threshold (0.0-1.0) to trigger scale-down.
    /// </summary>
    public double ScaleDownUtilizationThreshold { get; set; } = 0.25;

    /// <summary>
    /// Gets or sets the maximum average task duration to allow scale-down.
    /// </summary>
    public TimeSpan ScaleDownMaxTaskDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the number of workers to remove when scaling down.
    /// </summary>
    public int ScaleDownIncrement { get; set; } = 1;
}
