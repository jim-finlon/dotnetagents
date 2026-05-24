// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using DotNetAgents.Abstractions.PublicSubstitutes.Session;

namespace DotNetAgents.SessionPersistence.PublicSubstitutes.Session;

/// <summary>
/// Process-scoped public session store for examples, tests, and prototypes.
/// Snapshots are lost when the process exits.
/// </summary>
public sealed class InMemorySessionStore : IPublicSessionStore
{
    private readonly ConcurrentDictionary<SessionId, SessionSnapshot> _snapshots = new();

    public ValueTask<SessionSnapshot?> LoadAsync(SessionId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _snapshots.TryGetValue(id, out var snapshot);
        return ValueTask.FromResult(snapshot);
    }

    public ValueTask SaveAsync(
        SessionId id,
        SessionSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        cancellationToken.ThrowIfCancellationRequested();
        _snapshots[id] = snapshot with { Id = id };
        return ValueTask.CompletedTask;
    }

    public ValueTask DeleteAsync(SessionId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _snapshots.TryRemove(id, out _);
        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<SessionId> ListAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var id in _snapshots.Keys)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return id;
            await Task.Yield();
        }
    }
}
