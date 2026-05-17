namespace DotNetAgents.Agents.Tasks;

/// <summary>
/// Status of a worker task.
/// </summary>
public enum TaskStatus
{
    /// <summary>
    /// Task is pending and waiting to be assigned.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Task has been assigned to a worker agent.
    /// </summary>
    Assigned = 1,

    /// <summary>
    /// Task is currently being executed by a worker agent.
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// Task execution completed successfully.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Task execution failed.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Task execution was cancelled.
    /// </summary>
    Cancelled = 5
}
