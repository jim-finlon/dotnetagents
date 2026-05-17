using System.Collections.Concurrent;

namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// Story 1095b26a. Default <see cref="IPkceConsentStore"/> for single-replica
/// MCP servers. Concurrent-dictionary keyed on consent id; lookup via a
/// linear scan over (actor, client) which is fine at the operator-scale
/// volumes the consent surface sees. DB-backed implementations land in a
/// follow-up story for multi-replica deployments.
/// </summary>
public sealed class InMemoryPkceConsentStore : IPkceConsentStore
{
    private readonly ConcurrentDictionary<Guid, PkceConsentRecord> _records = new();
    private readonly TimeProvider _clock;

    public InMemoryPkceConsentStore(TimeProvider? clock = null)
    {
        _clock = clock ?? TimeProvider.System;
    }

    public Task RecordAsync(PkceConsentRecord decision, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(decision);
        if (string.IsNullOrWhiteSpace(decision.ActorId))
            throw new ArgumentException("ActorId is required.", nameof(decision));
        if (string.IsNullOrWhiteSpace(decision.ClientId))
            throw new ArgumentException("ClientId is required.", nameof(decision));

        // Last-write-wins: drop any prior decision for the same (actor, client) tuple,
        // then insert the new one. Lookup remains O(N) over a small set.
        foreach (var (id, existing) in _records)
        {
            if (string.Equals(existing.ActorId, decision.ActorId, StringComparison.Ordinal)
                && string.Equals(existing.ClientId, decision.ClientId, StringComparison.Ordinal))
            {
                _records.TryRemove(id, out _);
            }
        }

        _records[decision.Id] = decision;
        return Task.CompletedTask;
    }

    public Task<PkceConsentRecord?> FindCoveringAsync(
        string actorId,
        string clientId,
        IReadOnlyCollection<string> requestedScopes,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(actorId) || string.IsNullOrWhiteSpace(clientId))
            return Task.FromResult<PkceConsentRecord?>(null);

        var now = _clock.GetUtcNow();

        foreach (var record in _records.Values)
        {
            if (!string.Equals(record.ActorId, actorId, StringComparison.Ordinal)) continue;
            if (!string.Equals(record.ClientId, clientId, StringComparison.Ordinal)) continue;
            if (record.ExpiresAtUtc is { } expiry && expiry <= now) continue;
            if (record.Decision != PkceConsentDecision.Allow) continue;

            // Cover-check: every requested scope must be present in the recorded scope set.
            // Empty requestedScopes is trivially covered.
            if (requestedScopes.Count == 0
                || requestedScopes.All(scope => record.Scopes.Contains(scope, StringComparer.Ordinal)))
            {
                return Task.FromResult<PkceConsentRecord?>(record);
            }
        }

        return Task.FromResult<PkceConsentRecord?>(null);
    }

    public Task<IReadOnlyList<PkceConsentRecord>> ListAsync(string? actorIdFilter = null, CancellationToken cancellationToken = default)
    {
        var now = _clock.GetUtcNow();
        IEnumerable<PkceConsentRecord> records = _records.Values
            .Where(r => r.ExpiresAtUtc is null || r.ExpiresAtUtc.Value > now);

        if (!string.IsNullOrWhiteSpace(actorIdFilter))
        {
            records = records.Where(r => string.Equals(r.ActorId, actorIdFilter, StringComparison.Ordinal));
        }

        return Task.FromResult<IReadOnlyList<PkceConsentRecord>>(records
            .OrderByDescending(r => r.GrantedAtUtc)
            .ToList());
    }

    public Task RevokeAsync(Guid consentId, CancellationToken cancellationToken = default)
    {
        _records.TryRemove(consentId, out _);
        return Task.CompletedTask;
    }
}
