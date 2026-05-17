namespace DotNetAgents.Workflow.Session;

/// <summary>
/// Project-specific rules/content included in generated .cursorrules or agent instructions.
/// Defines coding standards, architectural patterns, and conventions for the project.
/// </summary>
public record ProjectRules
{
    /// <summary>
    /// Unique identifier for the rules record.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Session or project identifier (one-to-one with session).
    /// </summary>
    public string SessionId { get; init; } = string.Empty;

    /// <summary>
    /// Rules content (markdown/text) included in generated files.
    /// </summary>
    public string RulesContent { get; init; } = string.Empty;

    /// <summary>
    /// Format type for these rules (cursorrules, agent, or both).
    /// </summary>
    public RulesFormat FormatType { get; init; } = RulesFormat.Both;

    /// <summary>
    /// Categories/tags for organizing rule types (e.g., "coding", "commits", "security").
    /// </summary>
    public IReadOnlyList<string> Categories { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Optional metadata (version, author, lastReviewed, etc.).
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// When the rules were created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the rules were last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Format types for project rules.
/// </summary>
public enum RulesFormat
{
    /// <summary>
    /// Include in both .cursorrules and agent.md formats.
    /// </summary>
    Both = 0,

    /// <summary>
    /// Include only in .cursorrules format.
    /// </summary>
    CursorRules = 1,

    /// <summary>
    /// Include only in agent.md / CLAUDE.md format.
    /// </summary>
    Agent = 2
}
