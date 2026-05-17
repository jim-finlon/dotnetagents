namespace DotNetAgents.Tasks.Models;

/// <summary>
/// Represents an individual work item that can be tracked and managed.
/// </summary>
public record WorkTask
{
    /// <summary>
    /// Gets the unique identifier for the task.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the session identifier this task belongs to.
    /// </summary>
    public string SessionId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the workflow run identifier this task is associated with, if any.
    /// </summary>
    public string? WorkflowRunId { get; init; }

    /// <summary>
    /// Gets the task content/description.
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Gets the current status of the task.
    /// </summary>
    public TaskStatus Status { get; init; } = TaskStatus.Pending;

    /// <summary>
    /// Gets the priority level of the task.
    /// </summary>
    public TaskPriority Priority { get; init; } = TaskPriority.Medium;

    /// <summary>
    /// Gets the display order (0-based).
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// Gets the task IDs that must complete before this one.
    /// </summary>
    public IReadOnlyList<Guid> DependsOn { get; init; } = Array.Empty<Guid>();

    /// <summary>
    /// Gets the task IDs currently blocking this one.
    /// </summary>
    public IReadOnlyList<Guid> BlockedBy { get; init; } = Array.Empty<Guid>();

    /// <summary>
    /// Gets the parent task ID for hierarchical tasks/sub-tasks.
    /// </summary>
    public Guid? ParentTaskId { get; init; }

    /// <summary>
    /// Gets additional context or notes.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Gets tags for categorization and filtering.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets additional metadata for flexible extension.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets when the task was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets when the task was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets when work started on the task.
    /// </summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>
    /// Gets when the task was completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    /// Gets when the task was cancelled.
    /// </summary>
    public DateTimeOffset? CancelledAt { get; init; }
}
