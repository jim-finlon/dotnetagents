// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;

namespace DotNetAgents.PreviewConfirm;

/// <summary>Process-local store for tests and single-node services.</summary>
public sealed class InMemoryPreviewConfirmSessionStore : IPreviewConfirmSessionStore
{
    private readonly ConcurrentDictionary<Guid, PreviewConfirmSession> _sessions = new();

    public ValueTask SaveAsync(PreviewConfirmSession session, CancellationToken cancellationToken = default)
    {
        _sessions[session.SessionId] = session;
        return ValueTask.CompletedTask;
    }

    public ValueTask<PreviewConfirmSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryGetValue(sessionId, out var s);
        return ValueTask.FromResult(s);
    }

    public ValueTask<bool> TryRemoveAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(_sessions.TryRemove(sessionId, out _));
}
