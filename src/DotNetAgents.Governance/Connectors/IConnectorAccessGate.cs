// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Governance.Connectors;

/// <summary>
/// Gate consulted on every outbound connector call (MCP tool, HTTP integration, webhook).
/// Implementations look up an <see cref="ConnectorAllowlistEntry"/> for the
/// (agentId, invoker, connector) triple and return allow/deny. Default implementations
/// fail closed.
/// </summary>
public interface IConnectorAccessGate
{
    Task<ConnectorAccessDecision> CheckAsync(
        string agentId,
        string invokerUserId,
        string connectorId,
        CancellationToken ct);
}

/// <param name="Allowed">True when a valid, unexpired allowlist entry exists.</param>
/// <param name="Reason">Short human-readable explanation, populated on deny.</param>
public sealed record ConnectorAccessDecision(bool Allowed, string? Reason = null)
{
    public static ConnectorAccessDecision Allow() => new(true);
    public static ConnectorAccessDecision Deny(string reason) => new(false, reason);
}
