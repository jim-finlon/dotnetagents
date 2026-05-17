using System.Collections.Concurrent;

namespace DotNetAgents.Memory.Advanced;

/// <summary>In-memory triple store; query by substring match on S/P/O. FR-MEM-002.</summary>
public sealed class InMemorySemanticMemory : ISemanticMemory
{
    private readonly ConcurrentDictionary<string, Fact> _byId = new();
    private readonly List<Fact> _all = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public Task StoreFactAsync(Fact fact, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fact);
        var id = string.IsNullOrEmpty(fact.Id) ? Guid.NewGuid().ToString("N") : fact.Id;
        var f = fact with { Id = id };
        _byId[id] = f;
        lock (_lock) { _all.Add(f); }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Fact>> QueryAsync(string query, int limit = 50, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Task.FromResult<IReadOnlyList<Fact>>(Array.Empty<Fact>());

        var q = query.Trim();
        List<Fact> list;
        lock (_lock) { list = _all.ToList(); }

        var results = list
            .Where(f => f.Subject.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        f.Predicate.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        f.Object.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Take(limit)
            .ToList();
        return Task.FromResult<IReadOnlyList<Fact>>(results);
    }

    /// <inheritdoc />
    public Task UpdateBeliefAsync(string factId, double confidence, CancellationToken cancellationToken = default)
    {
        if (!_byId.TryGetValue(factId, out var existing)) return Task.CompletedTask;
        var updated = existing with { Confidence = Math.Clamp(confidence, 0, 1) };
        _byId[factId] = updated;
        lock (_lock)
        {
            var idx = _all.FindIndex(f => f.Id == factId);
            if (idx >= 0) _all[idx] = updated;
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ContradictionCheckAsync(Fact newFact, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(newFact);
        List<Fact> list;
        lock (_lock) { list = _all.ToList(); }

        var contradict = list.Any(f =>
            string.Equals(f.Subject, newFact.Subject, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(f.Predicate, newFact.Predicate, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(f.Object, newFact.Object, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(contradict);
    }
}
