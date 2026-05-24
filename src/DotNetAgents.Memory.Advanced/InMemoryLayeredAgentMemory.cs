// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;

namespace DotNetAgents.Memory.Advanced;

public sealed class InMemoryLayeredAgentMemory : ILayeredAgentMemory
{
    private readonly ConcurrentDictionary<string, AgentMemoryRecord> _records = new();
    private readonly TimeProvider _timeProvider;

    public InMemoryLayeredAgentMemory(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task<AgentMemoryRecord> StoreAsync(AgentMemoryRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(record.Content))
            throw new ArgumentException("Memory content is required.", nameof(record));

        var normalized = record with
        {
            Id = string.IsNullOrWhiteSpace(record.Id) ? Guid.NewGuid().ToString("n") : record.Id,
            CreatedAtUtc = record.CreatedAtUtc == default ? _timeProvider.GetUtcNow() : record.CreatedAtUtc,
            Importance = Math.Clamp(record.Importance, 0, 1),
            Labels = Normalize(record.Labels),
            Tags = Normalize(record.Tags),
            SourceRefs = Normalize(record.SourceRefs)
        };

        _records[normalized.Id] = normalized;
        return Task.FromResult(normalized);
    }

    public Task<IReadOnlyList<AgentMemoryRecord>> RetrieveAsync(AgentMemoryQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        var layers = query.Layers is { Count: > 0 } ? query.Layers.ToHashSet() : null;
        var labels = query.Labels is { Count: > 0 } ? query.Labels.ToHashSet() : null;
        var limit = query.Limit > 0 ? query.Limit : 20;

        var results = _records.Values
            .Where(record => layers is null || layers.Contains(record.Layer))
            .Where(record => labels is null || record.Labels.Any(labels.Contains))
            .Where(record => query.AgentId is null || record.AgentId is null || string.Equals(record.AgentId, query.AgentId, StringComparison.OrdinalIgnoreCase))
            .Where(record => query.ProjectId is null || record.ProjectId is null || string.Equals(record.ProjectId, query.ProjectId, StringComparison.OrdinalIgnoreCase))
            .Where(record => query.TaskId is null || record.TaskId is null || string.Equals(record.TaskId, query.TaskId, StringComparison.OrdinalIgnoreCase))
            .Where(record => MatchesText(record, query.Text))
            .OrderByDescending(record => record.Labels.Any(IsCriticalLabel))
            .ThenByDescending(record => record.Importance)
            .ThenBy(record => LayerRank(record.Layer))
            .ThenByDescending(record => record.CreatedAtUtc)
            .Take(limit)
            .ToArray();

        return Task.FromResult<IReadOnlyList<AgentMemoryRecord>>(results);
    }

    public AgentMemoryWritebackPlan PlanWriteback(AgentMemoryRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var labels = record.Labels.ToArray();
        var critical = labels.Any(IsCriticalLabel);
        var durable = critical || record.Importance >= 0.8 || record.Layer == AgentMemoryLayer.DurableLongTerm;
        var shared = record.Layer == AgentMemoryLayer.SharedProject || record.ProjectId is not null;

        var layers = new List<AgentMemoryLayer> { AgentMemoryLayer.CurrentTask };
        if (!string.IsNullOrWhiteSpace(record.AgentId))
            layers.Add(AgentMemoryLayer.AgentLocal);
        if (shared || critical)
            layers.Add(AgentMemoryLayer.SharedProject);
        if (durable)
            layers.Add(AgentMemoryLayer.DurableLongTerm);

        var recommended = labels.Length == 0 && durable
            ? new[] { AgentMemoryLabel.CriticalLesson }
            : labels;

        return new AgentMemoryWritebackPlan(
            layers.Distinct().ToArray(),
            recommended,
            RequiresDurableStore: durable,
            critical
                ? "Critical or hazardous memory must be written to shared and durable layers."
                : "Task memory can remain local unless importance or project scope requires sharing.");
    }

    private static bool MatchesText(AgentMemoryRecord record, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return true;

        return record.Content.Contains(text, StringComparison.OrdinalIgnoreCase) ||
               record.Tags.Any(tag => tag.Contains(text, StringComparison.OrdinalIgnoreCase)) ||
               record.SourceRefs.Any(source => source.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    private static int LayerRank(AgentMemoryLayer layer)
        => layer switch
        {
            AgentMemoryLayer.CurrentTask => 0,
            AgentMemoryLayer.AgentLocal => 1,
            AgentMemoryLayer.SharedProject => 2,
            AgentMemoryLayer.DurableLongTerm => 3,
            _ => 10
        };

    private static bool IsCriticalLabel(AgentMemoryLabel label)
        => label is AgentMemoryLabel.CriticalLesson
            or AgentMemoryLabel.Hazard
            or AgentMemoryLabel.AvoidPattern
            or AgentMemoryLabel.NotableFailure;

    private static IReadOnlyList<T> Normalize<T>(IEnumerable<T>? values)
        => values?.Distinct().ToArray() ?? Array.Empty<T>();
}
