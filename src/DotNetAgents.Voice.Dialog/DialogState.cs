namespace DotNetAgents.Voice.Dialog;

/// <summary>
/// Represents the state of a dialog session.
/// </summary>
public record DialogState
{
    /// <summary>
    /// Gets the unique identifier for the dialog.
    /// </summary>
    public Guid DialogId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the identifier of the user participating in the dialog.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Gets the type of dialog (e.g., "lifetime_goals", "activity_generation").
    /// </summary>
    public required string DialogType { get; init; }

    /// <summary>
    /// Gets the current status of the dialog.
    /// </summary>
    public DialogStatus Status { get; init; } = DialogStatus.Active;

    /// <summary>
    /// Gets the data collected during the dialog.
    /// </summary>
    public Dictionary<string, object> CollectedData { get; init; } = new();

    /// <summary>
    /// Gets the list of questions that still need to be asked.
    /// </summary>
    public List<string> PendingQuestions { get; init; } = new();

    /// <summary>
    /// Gets the current question being asked to the user.
    /// </summary>
    public string? CurrentQuestion { get; init; }

    /// <summary>
    /// Gets the timestamp when the dialog started.
    /// </summary>
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the timestamp when the dialog was completed.
    /// </summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when the dialog was last updated.
    /// </summary>
    public DateTime LastUpdatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets additional metadata for the dialog.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Represents the status of a dialog.
/// </summary>
public enum DialogStatus
{
    /// <summary>
    /// Dialog is active and waiting for input.
    /// </summary>
    Active,

    /// <summary>
    /// Dialog is waiting for user input.
    /// </summary>
    WaitingForInput,

    /// <summary>
    /// Dialog has been completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Dialog has been cancelled.
    /// </summary>
    Cancelled
}
