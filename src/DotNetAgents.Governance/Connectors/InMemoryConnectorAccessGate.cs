// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Governance.Connectors;

/// <summary>
/// In-memory <see cref="IConnectorAccessGate"/> for tests and early service adoption.
/// Production services should replace this with a backed-store implementation
/// (EF table, durable config, or centralized policy service).
/// </summary>
public sealed class InMemoryConnectorAccessGate : IConnectorAccessGate
{
    private readonly List<ConnectorAllowlistEntry> _entries;
    private readonly Func<DateTimeOffset> _now;

    public InMemoryConnectorAccessGate(
        IEnumerable<ConnectorAllowlistEntry>? seed = null,
        Func<DateTimeOffset>? now = null)
    {
        _entries = seed?.ToList() ?? new List<ConnectorAllowlistEntry>();
        _now = now ?? (() => DateTimeOffset.UtcNow);
    }

    public void Grant(ConnectorAllowlistEntry entry)
    {
        _entries.Add(entry);
    }

    public Task<ConnectorAccessDecision> CheckAsync(
        string agentId,
        string invokerUserId,
        string connectorId,
        CancellationToken ct)
    {
        var now = _now();
        foreach (var e in _entries)
        {
            if (!string.Equals(e.AgentId, agentId, StringComparison.Ordinal)) continue;
            if (!string.Equals(e.UserId, invokerUserId, StringComparison.Ordinal)) continue;
            if (!string.Equals(e.ConnectorId, connectorId, StringComparison.Ordinal)) continue;
            if (e.ExpiresAt is { } exp && exp <= now) continue;
            return Task.FromResult(ConnectorAccessDecision.Allow());
        }
        return Task.FromResult(
            ConnectorAccessDecision.Deny($"No active allowlist entry for agent='{agentId}', user='{invokerUserId}', connector='{connectorId}'."));
    }
}
