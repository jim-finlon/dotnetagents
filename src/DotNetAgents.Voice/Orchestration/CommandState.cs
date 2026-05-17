using DotNetAgents.Voice.IntentClassification;

namespace DotNetAgents.Voice.Orchestration;

/// <summary>
/// Represents the state of a voice command during processing.
/// </summary>
public record CommandState
{
    /// <summary>
    /// Gets the unique identifier for the command.
    /// </summary>
    public Guid CommandId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the identifier of the user who submitted the command.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Gets the raw voice command text.
    /// </summary>
    public required string RawText { get; init; }

    /// <summary>
    /// Gets the source of the command (e.g., "android", "ios", "web", "cli").
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Gets the current status of the command.
    /// </summary>
    public CommandStatus Status { get; init; } = CommandStatus.Queued;

    /// <summary>
    /// Gets the parsed intent (if available).
    /// </summary>
    public Intent? Intent { get; init; }

    /// <summary>
    /// Gets the target MCP service name (if available).
    /// </summary>
    public string? TargetService { get; init; }

    /// <summary>
    /// Gets a value indicating whether the command has been confirmed by the user.
    /// </summary>
    public bool Confirmed { get; init; }

    /// <summary>
    /// When true, downstream LLM steps should use verbose response style (higher token budget, permissive suffix).
    /// </summary>
    public bool? ResponseVerbose { get; init; }

    /// <summary>
    /// Optional durable user memory (preferences, about-me) to pass into intent classification and downstream steps.
    /// </summary>
    public string? UserMemoryContext { get; init; }

    /// <summary>
    /// Gets the timestamp when the command was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the timestamp when the command was confirmed.
    /// </summary>
    public DateTime? ConfirmedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when the command completed.
    /// </summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>
    /// Gets the result of the command execution.
    /// </summary>
    public object? Result { get; init; }

    /// <summary>
    /// Gets the error message (if the command failed).
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Gets the list of MCP calls made for this command.
    /// </summary>
    public List<McpCallResult> McpCalls { get; init; } = new();
}

/// <summary>
/// Represents the result of an MCP call.
/// </summary>
public record McpCallResult
{
    /// <summary>
    /// Gets the name of the MCP service.
    /// </summary>
    public required string Service { get; init; }

    /// <summary>
    /// Gets the name of the tool that was called.
    /// </summary>
    public required string Tool { get; init; }

    /// <summary>
    /// Gets the parameters passed to the tool.
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = new();

    /// <summary>
    /// Gets the result of the tool call.
    /// </summary>
    public object? Result { get; init; }

    /// <summary>
    /// Gets a value indicating whether the call was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the duration of the call in milliseconds.
    /// </summary>
    public int DurationMs { get; init; }

    /// <summary>
    /// Gets the timestamp when the call was made.
    /// </summary>
    public DateTime CalledAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the error message (if the call failed).
    /// </summary>
    public string? Error { get; init; }
}
