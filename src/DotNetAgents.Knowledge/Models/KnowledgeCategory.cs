namespace DotNetAgents.Knowledge.Models;

/// <summary>
/// Categorizes the type of knowledge item.
/// </summary>
public enum KnowledgeCategory
{
    /// <summary>
    /// Tool execution failed.
    /// </summary>
    ToolFailure = 0,

    /// <summary>
    /// Wrong strategy or approach.
    /// </summary>
    ApproachError = 1,

    /// <summary>
    /// Successful resolution to a problem.
    /// </summary>
    Solution = 2,

    /// <summary>
    /// Recommended pattern or practice.
    /// </summary>
    BestPractice = 3,

    /// <summary>
    /// Unexpected behavior or quirk.
    /// </summary>
    Gotcha = 4,

    /// <summary>
    /// Performance-related learning.
    /// </summary>
    Performance = 5,

    /// <summary>
    /// Security concern or vulnerability.
    /// </summary>
    Security = 6,

    /// <summary>
    /// Configuration or setup issue.
    /// </summary>
    Configuration = 7,

    /// <summary>
    /// General error or failure.
    /// </summary>
    Error = 8
}
