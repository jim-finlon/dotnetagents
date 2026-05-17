using System.Collections.Concurrent;

namespace DotNetAgents.Voice.Dialog;

/// <summary>
/// In-memory implementation of <see cref="IDialogStore"/> for testing and development.
/// </summary>
public class InMemoryDialogStore : IDialogStore
{
    private readonly ConcurrentDictionary<Guid, DialogState> _dialogs = new();

    /// <inheritdoc />
    public Task<DialogState> CreateAsync(
        DialogState state,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);

        _dialogs[state.DialogId] = state;
        return Task.FromResult(state);
    }

    /// <inheritdoc />
    public Task<DialogState?> GetAsync(
        Guid dialogId,
        CancellationToken cancellationToken = default)
    {
        _dialogs.TryGetValue(dialogId, out var state);
        return Task.FromResult<DialogState?>(state);
    }

    /// <inheritdoc />
    public Task<DialogState> UpdateAsync(
        DialogState state,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (!_dialogs.ContainsKey(state.DialogId))
        {
            throw new InvalidOperationException($"Dialog {state.DialogId} not found");
        }

        var updatedState = state with { LastUpdatedAt = DateTime.UtcNow };
        _dialogs[state.DialogId] = updatedState;
        return Task.FromResult(updatedState);
    }

    /// <inheritdoc />
    public Task<List<DialogState>> GetActiveDialogsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var activeDialogs = _dialogs.Values
            .Where(d => d.UserId == userId
                && (d.Status == DialogStatus.Active || d.Status == DialogStatus.WaitingForInput))
            .ToList();

        return Task.FromResult(activeDialogs);
    }

    /// <inheritdoc />
    public Task DeleteAsync(
        Guid dialogId,
        CancellationToken cancellationToken = default)
    {
        _dialogs.TryRemove(dialogId, out _);
        return Task.CompletedTask;
    }
}
