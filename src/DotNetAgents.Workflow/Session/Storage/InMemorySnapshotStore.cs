using DotNetAgents.Workflow.Session;

namespace DotNetAgents.Workflow.Session.Storage;

/// <summary>
/// In-memory implementation of <see cref="ISnapshotStore"/> for testing and development.
/// </summary>
public class InMemorySnapshotStore : ISnapshotStore
{
    private readonly Dictionary<Guid, WorkflowSnapshot> _snapshots = new();
    private readonly Dictionary<string, List<Guid>> _sessionSnapshots = new();
    private readonly Dictionary<string, int> _sessionSnapshotNumbers = new();
    private readonly object _lock = new();

    /// <inheritdoc/>
    public Task<WorkflowSnapshot> CreateAsync(WorkflowSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        if (snapshot == null)
            throw new ArgumentNullException(nameof(snapshot));

        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            // Auto-assign snapshot number if not set
            var snapshotNumber = snapshot.SnapshotNumber;
            if (snapshotNumber == 0)
            {
                if (!_sessionSnapshotNumbers.ContainsKey(snapshot.SessionId))
                {
                    _sessionSnapshotNumbers[snapshot.SessionId] = 0;
                }
                snapshotNumber = ++_sessionSnapshotNumbers[snapshot.SessionId];
            }

            var snapshotToCreate = snapshot with
            {
                Id = snapshot.Id == default ? Guid.NewGuid() : snapshot.Id,
                SnapshotNumber = snapshotNumber,
                CreatedAt = snapshot.CreatedAt == default ? DateTimeOffset.UtcNow : snapshot.CreatedAt
            };

            _snapshots[snapshotToCreate.Id] = snapshotToCreate;

            // Track by session
            if (!_sessionSnapshots.ContainsKey(snapshotToCreate.SessionId))
            {
                _sessionSnapshots[snapshotToCreate.SessionId] = new List<Guid>();
            }
            _sessionSnapshots[snapshotToCreate.SessionId].Add(snapshotToCreate.Id);

            return Task.FromResult(snapshotToCreate);
        }
    }

    /// <inheritdoc/>
    public Task<WorkflowSnapshot?> GetByIdAsync(Guid snapshotId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _snapshots.TryGetValue(snapshotId, out var snapshot);
            return Task.FromResult<WorkflowSnapshot?>(snapshot);
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<WorkflowSnapshot>> GetBySessionIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or whitespace.", nameof(sessionId));

        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (!_sessionSnapshots.TryGetValue(sessionId, out var snapshotIds))
            {
                return Task.FromResult<IReadOnlyList<WorkflowSnapshot>>(Array.Empty<WorkflowSnapshot>());
            }

            var snapshots = snapshotIds
                .Select(id => _snapshots.TryGetValue(id, out var snapshot) ? snapshot : null)
                .Where(s => s != null)
                .Cast<WorkflowSnapshot>()
                .OrderBy(s => s.SnapshotNumber)
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<WorkflowSnapshot>>(snapshots);
        }
    }

    /// <inheritdoc/>
    public Task<WorkflowSnapshot?> GetLatestAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or whitespace.", nameof(sessionId));

        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (!_sessionSnapshots.TryGetValue(sessionId, out var snapshotIds) || snapshotIds.Count == 0)
            {
                return Task.FromResult<WorkflowSnapshot?>(null);
            }

            var snapshots = snapshotIds
                .Select(id => _snapshots.TryGetValue(id, out var snapshot) ? snapshot : null)
                .Where(s => s != null)
                .Cast<WorkflowSnapshot>()
                .OrderByDescending(s => s.SnapshotNumber)
                .FirstOrDefault();

            return Task.FromResult<WorkflowSnapshot?>(snapshots);
        }
    }

    /// <inheritdoc/>
    public Task DeleteAsync(Guid snapshotId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (!_snapshots.TryGetValue(snapshotId, out var snapshot))
            {
                return Task.CompletedTask;
            }

            _snapshots.Remove(snapshotId);

            // Remove from session tracking
            if (_sessionSnapshots.TryGetValue(snapshot.SessionId, out var snapshotIds))
            {
                snapshotIds.Remove(snapshotId);
                if (snapshotIds.Count == 0)
                {
                    _sessionSnapshots.Remove(snapshot.SessionId);
                }
            }
        }

        return Task.CompletedTask;
    }
}
