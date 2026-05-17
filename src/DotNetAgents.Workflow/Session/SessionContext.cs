namespace DotNetAgents.Workflow.Session;

/// <summary>
/// Captures code-level and conversation-level context for a workflow session.
/// Provides additional context beyond tasks and knowledge to help resume work.
/// </summary>
public record SessionContext
{
    /// <summary>
    /// Gets the unique identifier for the session context.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the session identifier this context belongs to (one-to-one relationship).
    /// </summary>
    public string SessionId { get; init; } = string.Empty;

    /// <summary>
    /// Gets recent files that were being worked on (last 10-20 files).
    /// Helps understand what code areas were active.
    /// </summary>
    public IReadOnlyList<string> RecentFiles { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the most recently modified file path.
    /// </summary>
    public string? LastModifiedFile { get; init; }

    /// <summary>
    /// Gets the most recent git commit message.
    /// </summary>
    public string? LastCommitMessage { get; init; }

    /// <summary>
    /// Gets the most recent git commit hash.
    /// </summary>
    public string? LastCommitHash { get; init; }

    /// <summary>
    /// Gets key architecture and design decisions made during the session.
    /// </summary>
    public IReadOnlyList<string> KeyDecisions { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets open questions or unresolved issues.
    /// </summary>
    public IReadOnlyList<string> OpenQuestions { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets project assumptions and constraints.
    /// </summary>
    public IReadOnlyDictionary<string, string> Assumptions { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets recent commands or operations that were run.
    /// </summary>
    public IReadOnlyList<string> RecentCommands { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets recent errors or warnings encountered.
    /// Helps avoid repeating mistakes.
    /// </summary>
    public IReadOnlyList<string> RecentErrors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the current working directory or workspace path.
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Gets the active branch or environment.
    /// </summary>
    public string? ActiveBranch { get; init; }

    /// <summary>
    /// Gets additional metadata for flexible extension.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets when the context was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets when the context was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}
