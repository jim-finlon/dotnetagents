namespace DotNetAgents.Abstractions.Models;

/// <summary>
/// Represents a message in a chat conversation.
/// </summary>
public record ChatMessage
{
    /// <summary>
    /// Gets or sets the role of the message sender (e.g., "system", "user", "assistant", "tool").
    /// </summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the message sender (optional, used for multi-user conversations).
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets or sets additional metadata for the message.
    /// </summary>
    public IDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Creates a system message.
    /// </summary>
    /// <param name="content">The content of the system message.</param>
    /// <param name="name">Optional name for the system message.</param>
    /// <returns>A new system chat message.</returns>
    public static ChatMessage System(string content, string? name = null)
    {
        return new ChatMessage
        {
            Role = "system",
            Content = content,
            Name = name
        };
    }

    /// <summary>
    /// Creates a user message.
    /// </summary>
    /// <param name="content">The content of the user message.</param>
    /// <param name="name">Optional name for the user.</param>
    /// <returns>A new user chat message.</returns>
    public static ChatMessage User(string content, string? name = null)
    {
        return new ChatMessage
        {
            Role = "user",
            Content = content,
            Name = name
        };
    }

    /// <summary>
    /// Creates an assistant message.
    /// </summary>
    /// <param name="content">The content of the assistant message.</param>
    /// <param name="name">Optional name for the assistant.</param>
    /// <returns>A new assistant chat message.</returns>
    public static ChatMessage Assistant(string content, string? name = null)
    {
        return new ChatMessage
        {
            Role = "assistant",
            Content = content,
            Name = name
        };
    }

    /// <summary>
    /// Creates a tool message (for function calling).
    /// </summary>
    /// <param name="content">The content of the tool message.</param>
    /// <param name="toolCallId">The ID of the tool call this message responds to.</param>
    /// <param name="name">Optional name for the tool.</param>
    /// <returns>A new tool chat message.</returns>
    public static ChatMessage Tool(string content, string toolCallId, string? name = null)
    {
        return new ChatMessage
        {
            Role = "tool",
            Content = content,
            Name = name,
            Metadata = new Dictionary<string, object> { ["tool_call_id"] = toolCallId }
        };
    }
}
