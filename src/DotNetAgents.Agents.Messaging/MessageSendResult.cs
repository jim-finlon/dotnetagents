namespace DotNetAgents.Agents.Messaging;

/// <summary>
/// Result of sending a message.
/// </summary>
public record MessageSendResult
{
    /// <summary>
    /// Gets a value indicating whether the message was sent successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the error message if the send operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the ID of the message that was sent.
    /// </summary>
    public string MessageId { get; init; } = string.Empty;

    /// <summary>
    /// Creates a successful send result.
    /// </summary>
    /// <param name="messageId">The ID of the sent message.</param>
    /// <returns>A successful send result.</returns>
    public static MessageSendResult SuccessResult(string messageId) =>
        new()
        {
            Success = true,
            MessageId = messageId
        };

    /// <summary>
    /// Creates a failed send result.
    /// </summary>
    /// <param name="messageId">The ID of the message that failed to send.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failed send result.</returns>
    public static MessageSendResult FailureResult(string messageId, string errorMessage) =>
        new()
        {
            Success = false,
            MessageId = messageId,
            ErrorMessage = errorMessage
        };
}
