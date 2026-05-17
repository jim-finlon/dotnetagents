namespace DotNetAgents.Agents.WorkerPool;

/// <summary>
/// Statistics about worker pool.
/// </summary>
public record WorkerPoolStatistics
{
    /// <summary>
    /// Gets the total number of workers in the pool.
    /// </summary>
    public int TotalWorkers { get; init; }

    /// <summary>
    /// Gets the number of available workers.
    /// </summary>
    public int AvailableWorkers { get; init; }

    /// <summary>
    /// Gets the number of busy workers.
    /// </summary>
    public int BusyWorkers { get; init; }

    /// <summary>
    /// Gets the total number of tasks processed by the pool.
    /// </summary>
    public int TotalTasksProcessed { get; init; }

    /// <summary>
    /// Gets the average task duration.
    /// </summary>
    public TimeSpan AverageTaskDuration { get; init; }

    /// <summary>
    /// Gets the number of tasks processed by each worker.
    /// </summary>
    public Dictionary<string, int> TasksByWorker { get; init; } = new();
}
