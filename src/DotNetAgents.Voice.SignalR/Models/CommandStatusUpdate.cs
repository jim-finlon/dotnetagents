using DotNetAgents.Voice.Orchestration;

namespace DotNetAgents.Voice.SignalR.Models;

/// <summary>
/// Represents a real-time status update for a voice command.
/// </summary>
public record CommandStatusUpdate
{
    /// <summary>
    /// Gets the unique identifier for the command.
    /// </summary>
    public required Guid CommandId { get; init; }

    /// <summary>
    /// Gets the identifier of the user who submitted the command.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Gets the current status of the command.
    /// </summary>
    public required CommandStatus Status { get; init; }

    /// <summary>
    /// Gets the message describing the current state.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets the timestamp of the update.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the progress percentage (0-100) if applicable.
    /// </summary>
    public int? Progress { get; init; }

    /// <summary>
    /// Gets the result of the command (if completed).
    /// </summary>
    public object? Result { get; init; }

    /// <summary>
    /// Gets the error message (if failed).
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Gets additional metadata about the update.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Represents a clarification request for a voice command.
/// </summary>
public record ClarificationRequest
{
    /// <summary>
    /// Gets the unique identifier for the command.
    /// </summary>
    public required Guid CommandId { get; init; }

    /// <summary>
    /// Gets the identifier of the user who submitted the command.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Gets the prompt asking for clarification.
    /// </summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// Gets the name of the missing parameter.
    /// </summary>
    public required string MissingParameter { get; init; }

    /// <summary>
    /// Gets the current turn number in the clarification dialog.
    /// </summary>
    public int Turn { get; init; } = 1;

    /// <summary>
    /// Gets the maximum number of turns allowed.
    /// </summary>
    public int MaxTurns { get; init; } = 10;

    /// <summary>
    /// Gets the timestamp when the clarification was requested.
    /// </summary>
    public DateTime RequestedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a confirmation request for a voice command.
/// </summary>
public record ConfirmationRequest
{
    /// <summary>
    /// Gets the unique identifier for the command.
    /// </summary>
    public required Guid CommandId { get; init; }

    /// <summary>
    /// Gets the identifier of the user who submitted the command.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Gets the text to read back to the user for confirmation.
    /// </summary>
    public required string ReadBackText { get; init; }

    /// <summary>
    /// Gets the timestamp when the confirmation was requested.
    /// </summary>
    public DateTime RequestedAt { get; init; } = DateTime.UtcNow;
}
