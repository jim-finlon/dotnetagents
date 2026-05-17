namespace DotNetAgents.Voice.Scheduling;

/// <summary>
/// Interface for scheduling voice commands to execute at specific times.
/// </summary>
public interface ICommandScheduler
{
    /// <summary>
    /// Schedules a command to execute at a specific time.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="commandText">The command text to execute.</param>
    /// <param name="executeAt">The date and time when the command should execute.</param>
    /// <param name="context">Optional context data for the command execution.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The scheduled command identifier.</returns>
    Task<Guid> ScheduleCommandAsync(
        Guid userId,
        string commandText,
        DateTime executeAt,
        Dictionary<string, object>? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a recurring command.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="commandText">The command text to execute.</param>
    /// <param name="recurrence">The recurrence pattern.</param>
    /// <param name="context">Optional context data for the command execution.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The scheduled command identifier.</returns>
    Task<Guid> ScheduleRecurringCommandAsync(
        Guid userId,
        string commandText,
        RecurrencePattern recurrence,
        Dictionary<string, object>? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a scheduled command.
    /// </summary>
    /// <param name="scheduledCommandId">The scheduled command identifier.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    Task CancelScheduledCommandAsync(
        Guid scheduledCommandId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets scheduled commands for a user within a date range.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="from">Optional start date. If null, returns all future commands.</param>
    /// <param name="to">Optional end date. If null, returns all commands from start date.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The list of scheduled commands.</returns>
    Task<List<ScheduledCommand>> GetScheduledCommandsAsync(
        Guid userId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a scheduled command by ID.
    /// </summary>
    /// <param name="scheduledCommandId">The scheduled command identifier.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The scheduled command, or null if not found.</returns>
    Task<ScheduledCommand?> GetScheduledCommandAsync(
        Guid scheduledCommandId,
        CancellationToken cancellationToken = default);
}
