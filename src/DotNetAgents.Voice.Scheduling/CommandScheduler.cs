using Microsoft.Extensions.Logging;

namespace DotNetAgents.Voice.Scheduling;

/// <summary>
/// Default implementation of <see cref="ICommandScheduler"/>.
/// </summary>
public class CommandScheduler : ICommandScheduler
{
    private readonly IScheduledCommandStore _store;
    private readonly ILogger<CommandScheduler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandScheduler"/> class.
    /// </summary>
    /// <param name="store">The scheduled command store.</param>
    /// <param name="logger">The logger instance.</param>
    public CommandScheduler(
        IScheduledCommandStore store,
        ILogger<CommandScheduler> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Guid> ScheduleCommandAsync(
        Guid userId,
        string commandText,
        DateTime executeAt,
        Dictionary<string, object>? context = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(commandText))
        {
            throw new ArgumentException("Command text cannot be null or empty", nameof(commandText));
        }

        if (executeAt <= DateTime.UtcNow)
        {
            throw new ArgumentException("ExecuteAt must be in the future", nameof(executeAt));
        }

        var scheduledCommand = new ScheduledCommand
        {
            UserId = userId,
            CommandText = commandText,
            ExecuteAt = executeAt,
            Context = context ?? new Dictionary<string, object>(),
            Status = ScheduledCommandStatus.Pending
        };

        await _store.CreateAsync(scheduledCommand, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Scheduled command {CommandId} for user {UserId} to execute at {ExecuteAt}",
            scheduledCommand.Id,
            userId,
            executeAt);

        return scheduledCommand.Id;
    }

    /// <inheritdoc />
    public async Task<Guid> ScheduleRecurringCommandAsync(
        Guid userId,
        string commandText,
        RecurrencePattern recurrence,
        Dictionary<string, object>? context = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(commandText))
        {
            throw new ArgumentException("Command text cannot be null or empty", nameof(commandText));
        }

        ArgumentNullException.ThrowIfNull(recurrence);

        // Calculate first execution time based on recurrence pattern
        var firstExecuteAt = CalculateNextExecutionTime(DateTime.UtcNow, recurrence);

        var scheduledCommand = new ScheduledCommand
        {
            UserId = userId,
            CommandText = commandText,
            ExecuteAt = firstExecuteAt,
            Context = context ?? new Dictionary<string, object>(),
            Status = ScheduledCommandStatus.Pending,
            IsRecurring = true,
            Recurrence = recurrence
        };

        await _store.CreateAsync(scheduledCommand, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Scheduled recurring command {CommandId} for user {UserId} with pattern {RecurrenceType}",
            scheduledCommand.Id,
            userId,
            recurrence.Type);

        return scheduledCommand.Id;
    }

    /// <inheritdoc />
    public async Task CancelScheduledCommandAsync(
        Guid scheduledCommandId,
        CancellationToken cancellationToken = default)
    {
        var command = await _store.GetAsync(scheduledCommandId, cancellationToken).ConfigureAwait(false);
        if (command == null)
        {
            throw new InvalidOperationException($"Scheduled command {scheduledCommandId} not found");
        }

        if (command.Status == ScheduledCommandStatus.Completed
            || command.Status == ScheduledCommandStatus.Cancelled)
        {
            return; // Already completed or cancelled
        }

        var cancelledCommand = command with
        {
            Status = ScheduledCommandStatus.Cancelled
        };

        await _store.UpdateAsync(cancelledCommand, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Cancelled scheduled command {CommandId}", scheduledCommandId);
    }

    /// <inheritdoc />
    public Task<List<ScheduledCommand>> GetScheduledCommandsAsync(
        Guid userId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        return _store.GetByUserAndDateRangeAsync(userId, from, to, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledCommand?> GetScheduledCommandAsync(
        Guid scheduledCommandId,
        CancellationToken cancellationToken = default)
    {
        return _store.GetAsync(scheduledCommandId, cancellationToken);
    }

    private static DateTime CalculateNextExecutionTime(DateTime from, RecurrencePattern pattern)
    {
        return pattern.Type switch
        {
            RecurrenceType.Daily => from.AddDays(pattern.Interval),
            RecurrenceType.Weekly => from.AddDays(7 * pattern.Interval),
            RecurrenceType.Monthly => from.AddMonths(pattern.Interval),
            RecurrenceType.Yearly => from.AddYears(pattern.Interval),
            _ => throw new ArgumentException($"Unsupported recurrence type: {pattern.Type}", nameof(pattern))
        };
    }
}
