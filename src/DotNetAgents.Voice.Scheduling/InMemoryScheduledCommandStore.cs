using System.Collections.Concurrent;

namespace DotNetAgents.Voice.Scheduling;

/// <summary>
/// In-memory implementation of <see cref="IScheduledCommandStore"/> for testing and development.
/// </summary>
public class InMemoryScheduledCommandStore : IScheduledCommandStore
{
    private readonly ConcurrentDictionary<Guid, ScheduledCommand> _commands = new();

    /// <inheritdoc />
    public Task<ScheduledCommand> CreateAsync(
        ScheduledCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        _commands[command.Id] = command;
        return Task.FromResult(command);
    }

    /// <inheritdoc />
    public Task<ScheduledCommand?> GetAsync(
        Guid commandId,
        CancellationToken cancellationToken = default)
    {
        _commands.TryGetValue(commandId, out var command);
        return Task.FromResult<ScheduledCommand?>(command);
    }

    /// <inheritdoc />
    public Task<ScheduledCommand> UpdateAsync(
        ScheduledCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!_commands.ContainsKey(command.Id))
        {
            throw new InvalidOperationException($"Scheduled command {command.Id} not found");
        }

        _commands[command.Id] = command;
        return Task.FromResult(command);
    }

    /// <inheritdoc />
    public Task<List<ScheduledCommand>> GetDueCommandsAsync(
        DateTime upTo,
        CancellationToken cancellationToken = default)
    {
        var dueCommands = _commands.Values
            .Where(c => c.Status == ScheduledCommandStatus.Pending
                && c.ExecuteAt <= upTo)
            .OrderBy(c => c.ExecuteAt)
            .ToList();

        return Task.FromResult(dueCommands);
    }

    /// <inheritdoc />
    public Task<List<ScheduledCommand>> GetByUserAndDateRangeAsync(
        Guid userId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var query = _commands.Values.Where(c => c.UserId == userId);

        if (from.HasValue)
        {
            query = query.Where(c => c.ExecuteAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(c => c.ExecuteAt <= to.Value);
        }

        var commands = query.OrderBy(c => c.ExecuteAt).ToList();
        return Task.FromResult(commands);
    }

    /// <inheritdoc />
    public Task DeleteAsync(
        Guid commandId,
        CancellationToken cancellationToken = default)
    {
        _commands.TryRemove(commandId, out _);
        return Task.CompletedTask;
    }
}
