// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Text.Json;

namespace DotNetAgents.Memory.Advanced;

/// <summary>
/// In-memory working memory store with TTL and priority-based attention context.
/// Suitable for single-process agents. FR-MEM-004.
/// </summary>
public sealed class InMemoryWorkingMemory : IWorkingMemory
{
    private readonly ConcurrentDictionary<string, Entry> _store = new();
    private readonly ConcurrentDictionary<string, int> _priorities = new();
    private readonly int _attentionCapacity;
    private readonly TimeSpan? _defaultTtl;

    public InMemoryWorkingMemory(int attentionCapacity = 7, TimeSpan? defaultTtl = null)
    {
        _attentionCapacity = attentionCapacity > 0 ? attentionCapacity : 7;
        _defaultTtl = defaultTtl;
    }

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();
        if (!_store.TryGetValue(key, out var entry))
            return Task.FromResult<T?>(default);
        if (entry.ExpiresAt.HasValue && DateTime.UtcNow >= entry.ExpiresAt.Value)
        {
            _store.TryRemove(key, out _);
            _priorities.TryRemove(key, out _);
            return Task.FromResult<T?>(default);
        }
        try
        {
            var value = JsonSerializer.Deserialize<T>(entry.Json);
            return Task.FromResult(value);
        }
        catch
        {
            return Task.FromResult<T?>(default);
        }
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();
        var effectiveTtl = ttl ?? _defaultTtl;
        DateTime? expiresAt = effectiveTtl.HasValue ? DateTime.UtcNow.Add(effectiveTtl.Value) : null;
        var json = JsonSerializer.Serialize(value);
        _store[key] = new Entry(json, expiresAt);
        return Task.CompletedTask;
    }

    /// <summary>Set priority for a key (used by GetAttentionContextAsync). Higher = more attention.</summary>
    public void SetPriority(string key, int priority)
    {
        _priorities[key] = priority;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();
        _store.TryRemove(key, out _);
        _priorities.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<AttentionContext> GetAttentionContextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = DateTime.UtcNow;
        var valid = _store
            .Where(kv => !kv.Value.ExpiresAt.HasValue || now < kv.Value.ExpiresAt.Value)
            .Select(kv => (Key: kv.Key, Priority: _priorities.TryGetValue(kv.Key, out var p) ? p : 0))
            .OrderByDescending(x => x.Priority)
            .Take(_attentionCapacity)
            .Select(x => new AttentionItem(x.Key, x.Priority, Summary: null))
            .ToList();
        return Task.FromResult(new AttentionContext
        {
            Capacity = _attentionCapacity,
            Items = valid
        });
    }

    private sealed record Entry(string Json, DateTime? ExpiresAt);
}
