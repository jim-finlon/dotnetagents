namespace DotNetAgents.Voice.Orchestration;

/// <summary>
/// Represents the status of a voice command.
/// </summary>
public enum CommandStatus
{
    /// <summary>
    /// Command has been queued for processing.
    /// </summary>
    Queued,

    /// <summary>
    /// Command is being parsed.
    /// </summary>
    Parsing,

    /// <summary>
    /// Command is awaiting clarification for missing parameters.
    /// </summary>
    AwaitingClarification,

    /// <summary>
    /// Command is awaiting user confirmation.
    /// </summary>
    AwaitingConfirmation,

    /// <summary>
    /// Command has been confirmed by the user.
    /// </summary>
    Confirmed,

    /// <summary>
    /// Command is being processed.
    /// </summary>
    Processing,

    /// <summary>
    /// Command completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Command failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Command was cancelled.
    /// </summary>
    Cancelled
}
