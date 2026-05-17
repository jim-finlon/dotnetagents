namespace DotNetAgents.Tasks.Models;

/// <summary>
/// Represents statistics about tasks for reporting and analysis.
/// </summary>
public record TaskStatistics
{
    /// <summary>
    /// Gets the total number of tasks.
    /// </summary>
    public int Total { get; init; }

    /// <summary>
    /// Gets the number of pending tasks.
    /// </summary>
    public int Pending { get; init; }

    /// <summary>
    /// Gets the number of tasks in progress.
    /// </summary>
    public int InProgress { get; init; }

    /// <summary>
    /// Gets the number of completed tasks.
    /// </summary>
    public int Completed { get; init; }

    /// <summary>
    /// Gets the number of blocked tasks.
    /// </summary>
    public int Blocked { get; init; }

    /// <summary>
    /// Gets the number of tasks in review.
    /// </summary>
    public int Review { get; init; }

    /// <summary>
    /// Gets the number of cancelled tasks.
    /// </summary>
    public int Cancelled { get; init; }

    /// <summary>
    /// Gets the completion percentage (0-100).
    /// </summary>
    public double CompletionPercentage => Total > 0 ? (double)Completed / Total * 100 : 0;
}
