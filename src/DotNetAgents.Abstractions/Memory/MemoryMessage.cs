namespace DotNetAgents.Abstractions.Memory;

/// <summary>
/// Represents a message stored in memory.
/// </summary>
public record MemoryMessage
{
    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the role of the message (e.g., "user", "assistant", "system").
    /// </summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the message was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets additional metadata associated with the message.
    /// </summary>
    public IDictionary<string, object>? Metadata { get; init; }
}
