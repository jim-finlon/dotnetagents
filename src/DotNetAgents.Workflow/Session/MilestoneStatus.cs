namespace DotNetAgents.Workflow.Session;

/// <summary>
/// Represents the current status of a milestone.
/// </summary>
public enum MilestoneStatus
{
    /// <summary>
    /// Not started.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Currently in progress.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Successfully completed.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Cancelled or abandoned.
    /// </summary>
    Cancelled = 3
}
