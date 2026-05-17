namespace DotNetAgents.Voice.Dialog.StateMachines;

/// <summary>
/// Context object for dialog state machine operations.
/// </summary>
public class DialogContext
{
    /// <summary>
    /// Gets or sets the dialog identifier.
    /// </summary>
    public Guid DialogId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the dialog type.
    /// </summary>
    public string DialogType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of questions asked.
    /// </summary>
    public int QuestionsAsked { get; set; }

    /// <summary>
    /// Gets or sets the number of questions remaining.
    /// </summary>
    public int QuestionsRemaining { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether all required information has been collected.
    /// </summary>
    public bool AllInfoCollected { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether confirmation is required.
    /// </summary>
    public bool RequiresConfirmation { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the dialog has been confirmed.
    /// </summary>
    public bool Confirmed { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when collecting info started.
    /// </summary>
    public DateTimeOffset? CollectingInfoStartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when confirming started.
    /// </summary>
    public DateTimeOffset? ConfirmingStartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when executing started.
    /// </summary>
    public DateTimeOffset? ExecutingStartedAt { get; set; }

    /// <summary>
    /// Gets or sets the error count.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Gets or sets the last error message.
    /// </summary>
    public string? LastErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the dialog is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
