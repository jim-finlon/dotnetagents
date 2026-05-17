namespace DotNetAgents.Voice.Scheduling;

/// <summary>
/// Represents a command that is scheduled to execute at a specific time.
/// </summary>
public record ScheduledCommand
{
    /// <summary>
    /// Gets the unique identifier for the scheduled command.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the identifier of the user who scheduled the command.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Gets the command text to execute.
    /// </summary>
    public required string CommandText { get; init; }

    /// <summary>
    /// Gets the date and time when the command should be executed.
    /// </summary>
    public required DateTime ExecuteAt { get; init; }

    /// <summary>
    /// Gets additional context data for the command execution.
    /// </summary>
    public Dictionary<string, object> Context { get; init; } = new();

    /// <summary>
    /// Gets the current status of the scheduled command.
    /// </summary>
    public ScheduledCommandStatus Status { get; init; } = ScheduledCommandStatus.Pending;

    /// <summary>
    /// Gets the timestamp when the command was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the timestamp when the command was executed.
    /// </summary>
    public DateTime? ExecutedAt { get; init; }

    /// <summary>
    /// Gets the error message if execution failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the result of the command execution.
    /// </summary>
    public object? Result { get; init; }

    /// <summary>
    /// Gets a value indicating whether the command is recurring.
    /// </summary>
    public bool IsRecurring { get; init; }

    /// <summary>
    /// Gets the recurrence pattern if the command is recurring.
    /// </summary>
    public RecurrencePattern? Recurrence { get; init; }
}

/// <summary>
/// Represents the status of a scheduled command.
/// </summary>
public enum ScheduledCommandStatus
{
    /// <summary>
    /// Command is pending execution.
    /// </summary>
    Pending,

    /// <summary>
    /// Command is currently being executed.
    /// </summary>
    Executing,

    /// <summary>
    /// Command completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Command execution failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Command was cancelled.
    /// </summary>
    Cancelled
}

/// <summary>
/// Represents a recurrence pattern for scheduled commands.
/// </summary>
public record RecurrencePattern
{
    /// <summary>
    /// Gets the type of recurrence.
    /// </summary>
    public required RecurrenceType Type { get; init; }

    /// <summary>
    /// Gets the interval (e.g., every 2 days, every 3 weeks).
    /// </summary>
    public int Interval { get; init; } = 1;

    /// <summary>
    /// Gets the days of week for weekly recurrence.
    /// </summary>
    public DayOfWeek[]? DaysOfWeek { get; init; }

    /// <summary>
    /// Gets the day of month for monthly recurrence.
    /// </summary>
    public int? DayOfMonth { get; init; }

    /// <summary>
    /// Gets the end date for the recurrence, or null if it repeats indefinitely.
    /// </summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// Gets the maximum number of occurrences, or null if unlimited.
    /// </summary>
    public int? MaxOccurrences { get; init; }
}

/// <summary>
/// Represents the type of recurrence.
/// </summary>
public enum RecurrenceType
{
    /// <summary>
    /// Repeats daily.
    /// </summary>
    Daily,

    /// <summary>
    /// Repeats weekly.
    /// </summary>
    Weekly,

    /// <summary>
    /// Repeats monthly.
    /// </summary>
    Monthly,

    /// <summary>
    /// Repeats yearly.
    /// </summary>
    Yearly
}
