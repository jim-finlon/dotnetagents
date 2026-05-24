// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;

namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// Story 1095b26a. Default <see cref="IPkceConsentStore"/> for single-replica
/// MCP servers. Concurrent-dictionary keyed on consent id; lookup via a
/// linear scan over (actor, client, service) which is fine at the
/// operator-scale volumes the consent surface sees.
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

        var normalized = Normalize(decision);

        // Last-write-wins: revoke any prior active decision for the same
        // (actor, client, service) tuple, then insert the new one. Revoked
        // decisions remain available for audit when includeRevoked=true.
        foreach (var (id, existing) in _records)
        {
            if (IsSameConsentSubject(existing, normalized) && existing.RevokedAtUtc is null)
            {
                _records[id] = existing with { RevokedAtUtc = _clock.GetUtcNow() };
            }
        }

        _records[normalized.Id] = normalized;
        return Task.CompletedTask;
    }

    public Task<PkceConsentRecord?> FindCoveringAsync(
        string actorId,
        string clientId,
        IReadOnlyCollection<string> requestedScopes,
        CancellationToken cancellationToken = default,
        string? serviceName = null)
    {
        if (string.IsNullOrWhiteSpace(actorId) || string.IsNullOrWhiteSpace(clientId))
            return Task.FromResult<PkceConsentRecord?>(null);

        var now = _clock.GetUtcNow();

        foreach (var record in _records.Values)
        {
            if (!string.Equals(record.ActorId, actorId, StringComparison.Ordinal)) continue;
            if (!string.Equals(record.ClientId, clientId, StringComparison.Ordinal)) continue;
            if (!string.Equals(NormalizeServiceName(record.ServiceName), NormalizeServiceName(serviceName), StringComparison.Ordinal)) continue;
            if (record.RevokedAtUtc is not null) continue;
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

    public Task<IReadOnlyList<PkceConsentRecord>> ListAsync(
        string? actorIdFilter = null,
        bool includeRevoked = false,
        CancellationToken cancellationToken = default)
    {
        var now = _clock.GetUtcNow();
        IEnumerable<PkceConsentRecord> records = _records.Values
            .Where(r => r.ExpiresAtUtc is null || r.ExpiresAtUtc.Value > now);

        if (!includeRevoked)
        {
            records = records.Where(r => r.RevokedAtUtc is null);
        }

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
        if (_records.TryGetValue(consentId, out var existing) && existing.RevokedAtUtc is null)
        {
            _records[consentId] = existing with { RevokedAtUtc = _clock.GetUtcNow() };
        }

        return Task.CompletedTask;
    }

    private static PkceConsentRecord Normalize(PkceConsentRecord record) =>
        record with { ServiceName = NormalizeServiceName(record.ServiceName) };

    private static string NormalizeServiceName(string? serviceName) =>
        string.IsNullOrWhiteSpace(serviceName) ? "DNA MCP" : serviceName.Trim();

    private static bool IsSameConsentSubject(PkceConsentRecord left, PkceConsentRecord right) =>
        string.Equals(left.ActorId, right.ActorId, StringComparison.Ordinal)
        && string.Equals(left.ClientId, right.ClientId, StringComparison.Ordinal)
        && string.Equals(NormalizeServiceName(left.ServiceName), NormalizeServiceName(right.ServiceName), StringComparison.Ordinal);
}
