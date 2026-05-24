// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Memory.Advanced;

/// <summary>
/// Working memory: key-value store with optional TTL and attention context for agent focus.
/// FR-MEM-004.
/// </summary>
public interface IWorkingMemory
{
    /// <summary>Get a value by key. Returns default when missing or expired.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>Set a value with optional TTL. When ttl is null, the entry does not expire.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default);

    /// <summary>Remove a key.</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Get current attention context (focus items, capacity, priority). Used to build context for the LLM.</summary>
    Task<AttentionContext> GetAttentionContextAsync(CancellationToken cancellationToken = default);
}
