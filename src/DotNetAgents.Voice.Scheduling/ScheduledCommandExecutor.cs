using DotNetAgents.Voice.Orchestration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Voice.Scheduling;

/// <summary>
/// Background service that executes scheduled commands.
/// </summary>
public class ScheduledCommandExecutor : BackgroundService
{
    private readonly IScheduledCommandStore _store;
    private readonly ICommandWorkflowOrchestrator _orchestrator;
    private readonly ILogger<ScheduledCommandExecutor> _logger;
    private readonly TimeSpan _pollInterval;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledCommandExecutor"/> class.
    /// </summary>
    /// <param name="store">The scheduled command store.</param>
    /// <param name="orchestrator">The command workflow orchestrator.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="pollInterval">The interval between checking for due commands. Default is 1 minute.</param>
    public ScheduledCommandExecutor(
        IScheduledCommandStore store,
        ICommandWorkflowOrchestrator orchestrator,
        ILogger<ScheduledCommandExecutor> logger,
        TimeSpan? pollInterval = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pollInterval = pollInterval ?? TimeSpan.FromMinutes(1);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduled command executor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExecuteDueCommandsAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing scheduled commands");
            }

            await Task.Delay(_pollInterval, stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Scheduled command executor stopped");
    }

    private async Task ExecuteDueCommandsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var dueCommands = await _store.GetDueCommandsAsync(now, cancellationToken).ConfigureAwait(false);

        foreach (var command in dueCommands)
        {
            // Skip if already executing or cancelled
            if (command.Status != ScheduledCommandStatus.Pending)
            {
                continue;
            }

            // Mark as executing
            var executingCommand = command with
            {
                Status = ScheduledCommandStatus.Executing
            };
            await _store.UpdateAsync(executingCommand, cancellationToken).ConfigureAwait(false);

            // Execute in background to avoid blocking
            _ = Task.Run(async () =>
            {
                try
                {
                    await ExecuteCommandAsync(command, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to execute scheduled command {CommandId}", command.Id);
                    await HandleExecutionErrorAsync(command, ex, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }
    }

    private async Task ExecuteCommandAsync(
        ScheduledCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Executing scheduled command {CommandId}: {CommandText}",
            command.Id,
            command.CommandText);

        var commandState = new DotNetAgents.Voice.Orchestration.CommandState
        {
            UserId = command.UserId,
            RawText = command.CommandText,
            Source = "scheduled"
        };

        var result = await _orchestrator.ExecuteAsync(commandState, cancellationToken).ConfigureAwait(false);

        // Update command status
        var completedCommand = command with
        {
            Status = ScheduledCommandStatus.Completed,
            ExecutedAt = DateTime.UtcNow,
            Result = result
        };

        // If recurring, schedule next occurrence
        if (command.IsRecurring && command.Recurrence != null)
        {
            var nextExecuteAt = CalculateNextExecutionTime(command.ExecuteAt, command.Recurrence);

            // Check if recurrence should continue
            if (ShouldContinueRecurrence(command.Recurrence, nextExecuteAt))
            {
                var nextCommand = new ScheduledCommand
                {
                    UserId = command.UserId,
                    CommandText = command.CommandText,
                    ExecuteAt = nextExecuteAt,
                    Context = command.Context,
                    Status = ScheduledCommandStatus.Pending,
                    IsRecurring = true,
                    Recurrence = command.Recurrence
                };

                await _store.CreateAsync(nextCommand, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation(
                    "Scheduled next occurrence of recurring command {CommandId} for {ExecuteAt}",
                    command.Id,
                    nextExecuteAt);
            }
        }

        await _store.UpdateAsync(completedCommand, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Completed scheduled command {CommandId}", command.Id);
    }

    private async Task HandleExecutionErrorAsync(
        ScheduledCommand command,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var failedCommand = command with
        {
            Status = ScheduledCommandStatus.Failed,
            ExecutedAt = DateTime.UtcNow,
            ErrorMessage = exception.Message
        };

        await _store.UpdateAsync(failedCommand, cancellationToken).ConfigureAwait(false);
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

    private static bool ShouldContinueRecurrence(RecurrencePattern pattern, DateTime nextExecuteAt)
    {
        // Check end date
        if (pattern.EndDate.HasValue && nextExecuteAt > pattern.EndDate.Value)
        {
            return false;
        }

        // Max occurrences would need to be tracked separately
        // For now, we'll rely on end date

        return true;
    }
}
