// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.Memory;

namespace DotNetAgents.Core.Memory.Implementations;

/// <summary>
/// In-memory implementation of <see cref="IMemoryStore"/> that persists state in memory (non-persistent).
/// </summary>
public class InMemoryMemoryStore : InMemoryMemory, IMemoryStore
{
    private readonly object _saveLock = new();

    /// <inheritdoc/>
    public Task SaveAsync(CancellationToken cancellationToken = default)
    {
        // In-memory store doesn't actually persist, but we implement the interface
        // for testing and development purposes
        lock (_saveLock)
        {
            // No-op for in-memory implementation
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task LoadAsync(CancellationToken cancellationToken = default)
    {
        // In-memory store doesn't load from anywhere, but we implement the interface
        // for testing and development purposes
        return Task.CompletedTask;
    }
}
