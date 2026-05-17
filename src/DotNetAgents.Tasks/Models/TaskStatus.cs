namespace DotNetAgents.Tasks.Models;

/// <summary>
/// Represents the current status of a work task.
/// </summary>
public enum TaskStatus
{
    /// <summary>
    /// Not started.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Currently being worked on.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Cannot proceed due to dependency or issue.
    /// </summary>
    Blocked = 2,

    /// <summary>
    /// Awaiting verification or review.
    /// </summary>
    Review = 3,

    /// <summary>
    /// Successfully finished.
    /// </summary>
    Completed = 4,

    /// <summary>
    /// No longer needed or cancelled.
    /// </summary>
    Cancelled = 5
}
