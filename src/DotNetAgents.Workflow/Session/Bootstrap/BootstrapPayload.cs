namespace DotNetAgents.Workflow.Session.Bootstrap;

/// <summary>
/// Bootstrap payload container for session resumption.
/// </summary>
public record BootstrapPayload
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
    /// Gets the formatted content (JSON, markdown, etc.).
    /// </summary>
    public required string FormattedContent { get; init; }

    /// <summary>
    /// Gets the format of this payload.
    /// </summary>
    public BootstrapFormat Format { get; init; }

    /// <summary>
    /// Gets when this payload was generated.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets optional metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}
