namespace DotNetAgents.Agents.Supervisor;

/// <summary>
/// Context object for supervisor state machine operations.
/// </summary>
public class SupervisorContext
{
    /// <summary>
    /// Gets or sets the current number of tasks being processed.
    /// </summary>
    public int CurrentTaskCount { get; set; }

    /// <summary>
    /// Gets or sets the number of pending tasks.
    /// </summary>
    public int PendingTasks { get; set; }

    /// <summary>
    /// Gets or sets the number of available workers.
    /// </summary>
    public int AvailableWorkers { get; set; }

    /// <summary>
    /// Gets or sets the time of the last task delegation.
    /// </summary>
    public DateTimeOffset? LastDelegationTime { get; set; }

    /// <summary>
    /// Gets or sets the error count.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Gets or sets the last error message.
    /// </summary>
    public string? LastErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the supervisor agent ID.
    /// </summary>
    public string SupervisorId { get; set; } = "supervisor";
}
