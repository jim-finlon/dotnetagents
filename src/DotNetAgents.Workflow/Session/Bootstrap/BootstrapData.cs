using DotNetAgents.Workflow.Session;

namespace DotNetAgents.Workflow.Session.Bootstrap;

/// <summary>
/// Data container for bootstrap generation.
/// </summary>
public record BootstrapData
{
    /// <summary>
    /// Gets the session identifier.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Gets the workflow run identifier, if any.
    /// </summary>
    public string? WorkflowRunId { get; init; }

    /// <summary>
    /// Gets the resume point description.
    /// </summary>
    public required string ResumePoint { get; init; }

    /// <summary>
    /// Gets the session name or title.
    /// </summary>
    public string? SessionName { get; init; }

    /// <summary>
    /// Gets the session description.
    /// </summary>
    public string? SessionDescription { get; init; }

    /// <summary>
    /// Gets task summaries (optional, if Tasks package is integrated).
    /// </summary>
    public TaskSummaryData? TaskSummary { get; init; }

    /// <summary>
    /// Gets knowledge items (optional, if Knowledge package is integrated).
    /// </summary>
    public IReadOnlyList<KnowledgeItemData>? KnowledgeItems { get; init; }

    /// <summary>
    /// Gets milestones.
    /// </summary>
    public IReadOnlyList<Milestone> Milestones { get; init; } = Array.Empty<Milestone>();

    /// <summary>
    /// Gets the last snapshot, if any.
    /// </summary>
    public WorkflowSnapshot? LastSnapshot { get; init; }

    /// <summary>
    /// Gets the session context, if any.
    /// </summary>
    public SessionContext? SessionContext { get; init; }

    /// <summary>
    /// Gets project-specific rules to include in generated .cursorrules or agent instructions, if any.
    /// </summary>
    public ProjectRules? ProjectRules { get; init; }

    /// <summary>
    /// Gets optional metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Task summary data for bootstrap.
/// </summary>
public record TaskSummaryData
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
    /// Gets the completion percentage (0-100).
    /// </summary>
    public double CompletionPercentage { get; init; }

    /// <summary>
    /// Gets the task list.
    /// </summary>
    public IReadOnlyList<TaskItemData> Tasks { get; init; } = Array.Empty<TaskItemData>();
}

/// <summary>
/// Task item data for bootstrap.
/// </summary>
public record TaskItemData
{
    /// <summary>
    /// Gets the task identifier.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the task content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Gets the task status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets the task priority.
    /// </summary>
    public required string Priority { get; init; }

    /// <summary>
    /// Gets the task order.
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// Gets task tags.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets when the task was completed, if applicable.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }
}

/// <summary>
/// Knowledge item data for bootstrap.
/// </summary>
public record KnowledgeItemData
{
    /// <summary>
    /// Gets the knowledge item identifier.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the knowledge item title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the knowledge item description.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the knowledge category.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Gets the knowledge severity.
    /// </summary>
    public required string Severity { get; init; }

    /// <summary>
    /// Gets the solution, if any.
    /// </summary>
    public string? Solution { get; init; }

    /// <summary>
    /// Gets knowledge tags.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the reference count.
    /// </summary>
    public int ReferenceCount { get; init; }
}
