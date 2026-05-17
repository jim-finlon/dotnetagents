namespace DotNetAgents.Agents.Supervisor;

/// <summary>
/// Statistics about supervisor task execution.
/// </summary>
public record SupervisorStatistics
{
    /// <summary>
    /// Gets the total number of tasks submitted.
    /// </summary>
    public int TotalTasksSubmitted { get; init; }

    /// <summary>
    /// Gets the number of tasks that completed successfully.
    /// </summary>
    public int TasksCompleted { get; init; }

    /// <summary>
    /// Gets the number of tasks that failed.
    /// </summary>
    public int TasksFailed { get; init; }

    /// <summary>
    /// Gets the number of tasks pending execution.
    /// </summary>
    public int TasksPending { get; init; }

    /// <summary>
    /// Gets the number of tasks currently in progress.
    /// </summary>
    public int TasksInProgress { get; init; }

    /// <summary>
    /// Gets the average execution time for completed tasks.
    /// </summary>
    public TimeSpan AverageExecutionTime { get; init; }

    /// <summary>
    /// Gets the number of tasks grouped by task type.
    /// </summary>
    public Dictionary<string, int> TasksByType { get; init; } = new();

    /// <summary>
    /// Gets the number of tasks grouped by agent.
    /// </summary>
    public Dictionary<string, int> TasksByAgent { get; init; } = new();

    /// <summary>
    /// Gets the current state machine state, if state machine is enabled.
    /// </summary>
    public string? CurrentState { get; init; }
}
