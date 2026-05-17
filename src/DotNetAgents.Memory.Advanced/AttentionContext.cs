namespace DotNetAgents.Memory.Advanced;

/// <summary>
/// Current focus items for working memory: capacity, priority-ordered items, and size limits.
/// </summary>
public sealed record AttentionContext
{
    /// <summary>Maximum number of items that can be held in focus (capacity).</summary>
    public int Capacity { get; init; } = 7;

    /// <summary>Focus items in priority order (highest priority first). Key, priority (higher = more attention), optional summary.</summary>
    public IReadOnlyList<AttentionItem> Items { get; init; } = Array.Empty<AttentionItem>();

    /// <summary>Optional total size limit (e.g. character count) for eviction.</summary>
    public long? MaxTotalSize { get; init; }
}

/// <summary>Single item in attention context.</summary>
/// <param name="Key">Working memory key.</param>
/// <param name="Priority">Priority (higher = more attention).</param>
/// <param name="Summary">Optional short summary for context window.</param>
public sealed record AttentionItem(string Key, int Priority, string? Summary = null);
