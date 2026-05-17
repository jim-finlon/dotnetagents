namespace DotNetAgents.Workflow.Session;

/// <summary>
/// Captures the complete state of a workflow session at a point in time.
/// </summary>
public record WorkflowSnapshot
{
    /// <summary>
    /// Gets the unique identifier for the snapshot.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the session identifier this snapshot belongs to.
    /// </summary>
    public string SessionId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the workflow run identifier this snapshot is associated with, if any.
    /// </summary>
    public string? WorkflowRunId { get; init; }

    /// <summary>
    /// Gets the sequential snapshot number within the session (auto-incremented).
    /// </summary>
    public int SnapshotNumber { get; init; }

    /// <summary>
    /// Gets the workflow state at snapshot time (serialized).
    /// </summary>
    public string SerializedState { get; init; } = string.Empty;

    /// <summary>
    /// Gets the resume point description.
    /// </summary>
    public string ResumePoint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the task count summary at snapshot time.
    /// </summary>
    public SnapshotTaskSummary TaskSummary { get; init; } = new();

    /// <summary>
    /// Gets the full task list snapshot.
    /// </summary>
    public IReadOnlyList<TaskSnapshot> Tasks { get; init; } = Array.Empty<TaskSnapshot>();

    /// <summary>
    /// Gets the number of knowledge items at snapshot time.
    /// </summary>
    public int KnowledgeCount { get; init; }

    /// <summary>
    /// Gets what triggered this snapshot.
    /// </summary>
    public SnapshotTrigger Trigger { get; init; }

    /// <summary>
    /// Gets additional details about the trigger.
    /// </summary>
    public string? TriggerDetails { get; init; }

    /// <summary>
    /// Gets the approximate size in bytes.
    /// </summary>
    public long SizeInBytes { get; init; }

    /// <summary>
    /// Gets additional metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets when the snapshot was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Summary of task counts at snapshot time.
/// </summary>
public record SnapshotTaskSummary
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
    /// Gets the number of cancelled tasks.
    /// </summary>
    public int Cancelled { get; init; }
}

/// <summary>
/// Lightweight task snapshot (not the full WorkTask entity).
/// </summary>
public record TaskSnapshot
{
    /// <summary>
    /// Gets the task identifier.
    /// </summary>
    public Guid TaskId { get; init; }

    /// <summary>
    /// Gets the task content.
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Gets the task status.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets the task priority.
    /// </summary>
    public string Priority { get; init; } = string.Empty;

    /// <summary>
    /// Gets when the task was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets when the task was completed, if applicable.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }
}
