namespace DotNetAgents.Memory.Advanced;

/// <summary>Single episode for episodic memory. FR-MEM-001.</summary>
public sealed record Episode
{
    /// <summary>Unique id.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Short description (e.g. for display).</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Full context or content.</summary>
    public string Context { get; init; } = string.Empty;

    /// <summary>When the episode occurred.</summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>Optional embedding for similarity recall (set by store).</summary>
    public float[]? Embedding { get; init; }

    /// <summary>Optional importance score (0–1).</summary>
    public float? Importance { get; init; }
}
