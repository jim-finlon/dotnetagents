namespace DotNetAgents.Tasks.Models;

/// <summary>
/// Represents the priority level of a work task.
/// </summary>
public enum TaskPriority
{
    /// <summary>
    /// Low priority.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Medium priority (default).
    /// </summary>
    Medium = 1,

    /// <summary>
    /// High priority.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority - must be done first.
    /// </summary>
    Critical = 3
}
