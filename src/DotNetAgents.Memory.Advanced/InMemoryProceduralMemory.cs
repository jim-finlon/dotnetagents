using System.Collections.Concurrent;

namespace DotNetAgents.Memory.Advanced;

/// <summary>In-memory procedure store; similar search by goal substring. FR-MEM-003.</summary>
public sealed class InMemoryProceduralMemory : IProceduralMemory
{
    private readonly ConcurrentDictionary<string, Procedure> _byName = new();
    private readonly List<Procedure> _all = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public Task LearnProcedureAsync(string name, string goal, IReadOnlyList<Step> steps, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var id = Guid.NewGuid().ToString("N");
        var p = new Procedure { Id = id, Name = name, Goal = goal ?? string.Empty, Steps = steps ?? Array.Empty<Step>() };
        _byName[name] = p;
        lock (_lock) { _all.Add(p); }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Procedure?> RecallProcedureAsync(string name, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_byName.TryGetValue(name, out var p) ? p : null);
    }

    /// <inheritdoc />
    public Task RefineProcedureAsync(string name, string feedback, CancellationToken cancellationToken = default)
    {
        if (!_byName.TryGetValue(name, out var existing)) return Task.CompletedTask;
        var refined = existing with { Goal = (existing.Goal + " " + (feedback ?? string.Empty)).Trim() };
        _byName[name] = refined;
        lock (_lock)
        {
            var idx = _all.FindIndex(p => p.Name == name);
            if (idx >= 0) _all[idx] = refined;
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Procedure>> FindSimilarProceduresAsync(string goal, int limit = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(goal))
            return Task.FromResult<IReadOnlyList<Procedure>>(Array.Empty<Procedure>());

        List<Procedure> list;
        lock (_lock) { list = _all.ToList(); }

        var results = list
            .Where(p => p.Goal.Contains(goal, StringComparison.OrdinalIgnoreCase))
            .Take(limit)
            .ToList();
        return Task.FromResult<IReadOnlyList<Procedure>>(results);
    }
}
