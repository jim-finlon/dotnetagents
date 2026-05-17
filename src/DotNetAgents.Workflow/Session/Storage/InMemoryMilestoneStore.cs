using DotNetAgents.Workflow.Session;

namespace DotNetAgents.Workflow.Session.Storage;

/// <summary>
/// In-memory implementation of <see cref="IMilestoneStore"/> for testing and development.
/// </summary>
public class InMemoryMilestoneStore : IMilestoneStore
{
    private readonly Dictionary<Guid, Milestone> _milestones = new();
    private readonly Dictionary<string, List<Guid>> _sessionMilestones = new();
    private readonly object _lock = new();

    /// <inheritdoc/>
    public Task<Milestone> CreateAsync(Milestone milestone, CancellationToken cancellationToken = default)
    {
        if (milestone == null)
            throw new ArgumentNullException(nameof(milestone));

        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var milestoneToCreate = milestone with
            {
                Id = milestone.Id == default ? Guid.NewGuid() : milestone.Id,
                CreatedAt = milestone.CreatedAt == default ? DateTimeOffset.UtcNow : milestone.CreatedAt,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _milestones[milestoneToCreate.Id] = milestoneToCreate;

            // Track by session
            if (!_sessionMilestones.ContainsKey(milestoneToCreate.SessionId))
            {
                _sessionMilestones[milestoneToCreate.SessionId] = new List<Guid>();
            }
            _sessionMilestones[milestoneToCreate.SessionId].Add(milestoneToCreate.Id);

            return Task.FromResult(milestoneToCreate);
        }
    }

    /// <inheritdoc/>
    public Task<Milestone?> GetByIdAsync(Guid milestoneId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _milestones.TryGetValue(milestoneId, out var milestone);
            return Task.FromResult<Milestone?>(milestone);
        }
    }

    /// <inheritdoc/>
    public Task<Milestone> UpdateAsync(Milestone milestone, CancellationToken cancellationToken = default)
    {
        if (milestone == null)
            throw new ArgumentNullException(nameof(milestone));

        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (!_milestones.ContainsKey(milestone.Id))
            {
                throw new InvalidOperationException($"Milestone {milestone.Id} not found.");
            }

            var updatedMilestone = milestone with
            {
                UpdatedAt = DateTimeOffset.UtcNow,
                CompletedAt = GetCompletedAt(_milestones[milestone.Id], milestone)
            };

            _milestones[milestone.Id] = updatedMilestone;
            return Task.FromResult(updatedMilestone);
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Milestone>> GetBySessionIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or whitespace.", nameof(sessionId));

        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (!_sessionMilestones.TryGetValue(sessionId, out var milestoneIds))
            {
                return Task.FromResult<IReadOnlyList<Milestone>>(Array.Empty<Milestone>());
            }

            var milestones = milestoneIds
                .Select(id => _milestones.TryGetValue(id, out var milestone) ? milestone : null)
                .Where(m => m != null)
                .Cast<Milestone>()
                .OrderBy(m => m.Order)
                .ThenBy(m => m.CreatedAt)
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<Milestone>>(milestones);
        }
    }

    /// <inheritdoc/>
    public Task DeleteAsync(Guid milestoneId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (!_milestones.TryGetValue(milestoneId, out var milestone))
            {
                return Task.CompletedTask;
            }

            _milestones.Remove(milestoneId);

            // Remove from session tracking
            if (_sessionMilestones.TryGetValue(milestone.SessionId, out var milestoneIds))
            {
                milestoneIds.Remove(milestoneId);
                if (milestoneIds.Count == 0)
                {
                    _sessionMilestones.Remove(milestone.SessionId);
                }
            }
        }

        return Task.CompletedTask;
    }

    private static DateTimeOffset? GetCompletedAt(Milestone existing, Milestone updated)
    {
        if (updated.Status == MilestoneStatus.Completed && existing.Status != MilestoneStatus.Completed)
        {
            return updated.CompletedAt ?? DateTimeOffset.UtcNow;
        }
        return updated.CompletedAt ?? existing.CompletedAt;
    }
}
