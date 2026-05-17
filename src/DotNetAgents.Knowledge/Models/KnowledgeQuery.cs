namespace DotNetAgents.Knowledge.Models;

/// <summary>
/// Query parameters for filtering and searching knowledge items.
/// </summary>
public record KnowledgeQuery
{
    /// <summary>
    /// Gets the session identifier filter (null for global knowledge only).
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// Gets the category filter.
    /// </summary>
    public KnowledgeCategory? Category { get; init; }

    /// <summary>
    /// Gets the severity filter.
    /// </summary>
    public KnowledgeSeverity? Severity { get; init; }

    /// <summary>
    /// Gets tags to filter by.
    /// </summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>
    /// Gets the search text for full-text search.
    /// </summary>
    public string? SearchText { get; init; }

    /// <summary>
    /// Gets whether to include global knowledge items.
    /// </summary>
    public bool IncludeGlobal { get; init; } = true;

    /// <summary>
    /// Gets the page number (1-based).
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Gets the field to sort by.
    /// </summary>
    public string SortBy { get; init; } = "CreatedAt";

    /// <summary>
    /// Gets whether to sort in descending order.
    /// </summary>
    public bool SortDescending { get; init; } = true;
}
