namespace DotNetAgents.Workflow.Session;

/// <summary>
/// Tracks significant achievements or checkpoints in a workflow session.
/// </summary>
public record Milestone
{
    /// <summary>
    /// Gets the unique identifier for the milestone.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the session identifier this milestone belongs to.
    /// </summary>
    public string SessionId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the workflow run identifier this milestone is associated with, if any.
    /// </summary>
    public string? WorkflowRunId { get; init; }

    /// <summary>
    /// Gets the milestone name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the milestone description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the current status of the milestone.
    /// </summary>
    public MilestoneStatus Status { get; init; } = MilestoneStatus.Pending;

    /// <summary>
    /// Gets the task IDs that must be completed for this milestone.
    /// </summary>
    public IReadOnlyList<Guid> RequiredTaskIds { get; init; } = Array.Empty<Guid>();

    /// <summary>
    /// Gets custom completion criteria (key-value pairs).
    /// </summary>
    public IReadOnlyDictionary<string, string> Criteria { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets the display order for sorting milestones.
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// Gets tags for categorization.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets additional metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets when the milestone was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets when the milestone was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets when the milestone was completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    /// Gets the optional target completion date.
    /// </summary>
    public DateTimeOffset? DueDate { get; init; }

    /// <summary>
    /// Gets the optional snapshot taken at milestone completion.
    /// </summary>
    public Guid? SnapshotId { get; init; }
}
