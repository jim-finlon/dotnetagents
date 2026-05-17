namespace DotNetAgents.Workflow.Checkpoints;

/// <summary>
/// In-memory implementation of <see cref="ICheckpointStore{TState}"/> for testing and development.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public class InMemoryCheckpointStore<TState> : ICheckpointStore<TState> where TState : class
{
    private readonly Dictionary<string, Checkpoint<TState>> _checkpoints = new();
    private readonly Dictionary<string, List<string>> _runCheckpoints = new();
    private readonly object _lock = new();

    /// <inheritdoc/>
    public Task<string> SaveAsync(
        Checkpoint<TState> checkpoint,
        CancellationToken cancellationToken = default)
    {
        if (checkpoint == null)
            throw new ArgumentNullException(nameof(checkpoint));

        if (string.IsNullOrWhiteSpace(checkpoint.Id))
        {
            throw new ArgumentException("Checkpoint ID cannot be null or whitespace.", nameof(checkpoint));
        }

        lock (_lock)
        {
            _checkpoints[checkpoint.Id] = checkpoint;

            if (!_runCheckpoints.ContainsKey(checkpoint.RunId))
            {
                _runCheckpoints[checkpoint.RunId] = new List<string>();
            }

            if (!_runCheckpoints[checkpoint.RunId].Contains(checkpoint.Id))
            {
                _runCheckpoints[checkpoint.RunId].Add(checkpoint.Id);
            }
        }

        return Task.FromResult(checkpoint.Id);
    }

    /// <inheritdoc/>
    public Task<Checkpoint<TState>?> GetAsync(
        string checkpointId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(checkpointId))
            throw new ArgumentException("Checkpoint ID cannot be null or whitespace.", nameof(checkpointId));

        lock (_lock)
        {
            _checkpoints.TryGetValue(checkpointId, out var checkpoint);
            return Task.FromResult<Checkpoint<TState>?>(checkpoint);
        }
    }

    /// <inheritdoc/>
    public Task<Checkpoint<TState>?> GetLatestAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId))
            throw new ArgumentException("Run ID cannot be null or whitespace.", nameof(runId));

        lock (_lock)
        {
            if (!_runCheckpoints.TryGetValue(runId, out var checkpointIds) || checkpointIds.Count == 0)
            {
                return Task.FromResult<Checkpoint<TState>?>(null);
            }

            // Get the most recent checkpoint by CreatedAt
            var latestCheckpoint = checkpointIds
                .Select(id => _checkpoints.TryGetValue(id, out var cp) ? cp : null)
                .Where(cp => cp != null)
                .OrderByDescending(cp => cp!.CreatedAt)
                .FirstOrDefault();

            return Task.FromResult<Checkpoint<TState>?>(latestCheckpoint);
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Checkpoint<TState>>> ListAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId))
            throw new ArgumentException("Run ID cannot be null or whitespace.", nameof(runId));

        lock (_lock)
        {
            if (!_runCheckpoints.TryGetValue(runId, out var checkpointIds))
            {
                return Task.FromResult<IReadOnlyList<Checkpoint<TState>>>(Array.Empty<Checkpoint<TState>>());
            }

            var checkpoints = checkpointIds
                .Select(id => _checkpoints.TryGetValue(id, out var cp) ? cp : null)
                .Where(cp => cp != null)
                .Cast<Checkpoint<TState>>()
                .OrderBy(cp => cp.CreatedAt)
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<Checkpoint<TState>>>(checkpoints);
        }
    }

    /// <inheritdoc/>
    public Task DeleteAsync(
        string checkpointId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(checkpointId))
            throw new ArgumentException("Checkpoint ID cannot be null or whitespace.", nameof(checkpointId));

        lock (_lock)
        {
            if (_checkpoints.TryGetValue(checkpointId, out var checkpoint))
            {
                _checkpoints.Remove(checkpointId);

                if (_runCheckpoints.TryGetValue(checkpoint.RunId, out var checkpointIds))
                {
                    checkpointIds.Remove(checkpointId);
                    if (checkpointIds.Count == 0)
                    {
                        _runCheckpoints.Remove(checkpoint.RunId);
                    }
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<int> DeleteOlderThanAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var toDelete = _checkpoints.Values
                .Where(cp => cp.CreatedAt < olderThan)
                .Select(cp => cp.Id)
                .ToList();

            foreach (var id in toDelete)
            {
                if (_checkpoints.TryGetValue(id, out var checkpoint))
                {
                    _checkpoints.Remove(id);

                    if (_runCheckpoints.TryGetValue(checkpoint.RunId, out var checkpointIds))
                    {
                        checkpointIds.Remove(id);
                        if (checkpointIds.Count == 0)
                        {
                            _runCheckpoints.Remove(checkpoint.RunId);
                        }
                    }
                }
            }

            return Task.FromResult(toDelete.Count);
        }
    }
}
