namespace DotNetAgents.Voice.StateMachines;

/// <summary>
/// Context object for voice session state machine operations.
/// </summary>
public class VoiceSessionContext
{
    /// <summary>
    /// Gets or sets the session identifier.
    /// </summary>
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the current command identifier (if processing a command).
    /// </summary>
    public Guid? CurrentCommandId { get; set; }

    /// <summary>
    /// Gets or sets the raw voice input text.
    /// </summary>
    public string? VoiceInput { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when listening started.
    /// </summary>
    public DateTimeOffset? ListeningStartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when processing started.
    /// </summary>
    public DateTimeOffset? ProcessingStartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when responding started.
    /// </summary>
    public DateTimeOffset? RespondingStartedAt { get; set; }

    /// <summary>
    /// Gets or sets the error count.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Gets or sets the last error message.
    /// </summary>
    public string? LastErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the session is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
