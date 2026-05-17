namespace DotNetAgents.Knowledge.Models;

/// <summary>
/// Represents the severity level of a knowledge item.
/// </summary>
public enum KnowledgeSeverity
{
    /// <summary>
    /// Informational only.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Minor issue or concern.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Significant problem.
    /// </summary>
    Error = 2,

    /// <summary>
    /// Major blocker or critical issue.
    /// </summary>
    Critical = 3
}
