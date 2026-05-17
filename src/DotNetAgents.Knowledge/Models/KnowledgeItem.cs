namespace DotNetAgents.Knowledge.Models;

/// <summary>
/// Captures learning from successes, failures, and discoveries during agent execution.
/// Can be session-specific or global (shared across all sessions).
/// </summary>
public record KnowledgeItem
{
    /// <summary>
    /// Gets the unique identifier for the knowledge item.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the session identifier this knowledge item belongs to (null for global knowledge).
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// Gets the workflow run identifier this knowledge item is associated with, if any.
    /// </summary>
    public string? WorkflowRunId { get; init; }

    /// <summary>
    /// Gets the optional link to a specific task.
    /// </summary>
    public Guid? TaskId { get; init; }

    /// <summary>
    /// Gets the knowledge item title.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Gets the detailed description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets what was being attempted (context).
    /// </summary>
    public string? Context { get; init; }

    /// <summary>
    /// Gets how it was resolved (solution).
    /// </summary>
    public string? Solution { get; init; }

    /// <summary>
    /// Gets the category of the knowledge item.
    /// </summary>
    public KnowledgeCategory Category { get; init; }

    /// <summary>
    /// Gets the severity level of the knowledge item.
    /// </summary>
    public KnowledgeSeverity Severity { get; init; } = KnowledgeSeverity.Info;

    /// <summary>
    /// Gets tags for searching and filtering.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets tech stack technologies this knowledge item is relevant for.
    /// Used for relevance matching when querying knowledge.
    /// </summary>
    public IReadOnlyList<string> TechStack { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the name of the session this knowledge item was imported from (for tracking origin).
    /// </summary>
    public string? SourceSession { get; init; }

    /// <summary>
    /// Gets when this knowledge item was imported from an external source (null if created directly).
    /// </summary>
    public DateTimeOffset? ImportedAt { get; init; }

    /// <summary>
    /// Gets the source identifier for imported knowledge items (file path, URL, etc.).
    /// </summary>
    public string? ImportSource { get; init; }

    /// <summary>
    /// Gets the error message if capturing a failure.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the stack trace if applicable.
    /// </summary>
    public string? StackTrace { get; init; }

    /// <summary>
    /// Gets the tool name that failed or succeeded.
    /// </summary>
    public string? ToolName { get; init; }

    /// <summary>
    /// Gets the tool parameters that were used.
    /// </summary>
    public IReadOnlyDictionary<string, object> ToolParameters { get; init; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets how many times this knowledge item has been referenced.
    /// </summary>
    public int ReferenceCount { get; init; }

    /// <summary>
    /// Gets whether this is a global knowledge item (not tied to any session).
    /// </summary>
    public bool IsGlobal => SessionId == null;

    /// <summary>
    /// Gets additional metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets when the knowledge item was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets when the knowledge item was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets when the knowledge item was last referenced.
    /// </summary>
    public DateTimeOffset? LastReferencedAt { get; init; }

    /// <summary>
    /// Gets the SHA256 hash of title + description (first 500 chars) for duplicate detection.
    /// Used for fast O(1) duplicate checking instead of O(n) comparison.
    /// </summary>
    public string? ContentHash { get; init; }
}
