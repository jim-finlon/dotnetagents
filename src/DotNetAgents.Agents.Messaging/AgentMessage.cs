namespace DotNetAgents.Agents.Messaging;

/// <summary>
/// Represents a message between agents.
/// </summary>
public record AgentMessage
{
    /// <summary>
    /// Gets the unique identifier of the message.
    /// </summary>
    public string MessageId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets the ID of the agent sending the message.
    /// </summary>
    public string FromAgentId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the ID of the agent receiving the message, or "*" for broadcast.
    /// </summary>
    public string ToAgentId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the type of the message (e.g., "task_request", "task_response", "status_update").
    /// </summary>
    public string MessageType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the message payload.
    /// </summary>
    public object Payload { get; init; } = new();

    /// <summary>
    /// Gets additional message headers.
    /// </summary>
    public Dictionary<string, string> Headers { get; init; } = new();

    /// <summary>
    /// Gets the timestamp when the message was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the correlation ID for tracking related messages.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets the time-to-live for the message (optional).
    /// </summary>
    public TimeSpan? TimeToLive { get; init; }
}
