using System.Collections.Concurrent;

namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// Default <see cref="IPkceChallengeStore"/> for single-replica MCP servers. Concurrent
/// dictionary keyed on authorization code; expired entries are evicted lazily on read.
/// </summary>
public sealed class InMemoryPkceChallengeStore : IPkceChallengeStore
{
    private readonly ConcurrentDictionary<string, PkceChallengeRecord> _records = new(StringComparer.Ordinal);
    private readonly TimeProvider _clock;

    public InMemoryPkceChallengeStore(TimeProvider? clock = null)
    {
        _clock = clock ?? TimeProvider.System;
    }

    public Task StoreAsync(string authorizationCode, PkceChallengeRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorizationCode);
        ArgumentNullException.ThrowIfNull(record);
        _records[authorizationCode] = record;
        return Task.CompletedTask;
    }

    public Task<PkceChallengeRecord?> ConsumeAsync(string authorizationCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(authorizationCode))
        {
            return Task.FromResult<PkceChallengeRecord?>(null);
        }

        if (!_records.TryRemove(authorizationCode, out var record))
        {
            return Task.FromResult<PkceChallengeRecord?>(null);
        }

        if (record.ExpiresAtUtc <= _clock.GetUtcNow())
        {
            return Task.FromResult<PkceChallengeRecord?>(null);
        }

        return Task.FromResult<PkceChallengeRecord?>(record);
    }
}
