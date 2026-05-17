namespace DotNetAgents.Governance.Connectors;

/// <summary>
/// A single grant: user <paramref name="UserId"/> has allowed agent <paramref name="AgentId"/>
/// to reach connector <paramref name="ConnectorId"/>. Services persist these records and an
/// <see cref="IConnectorAccessGate"/> consults them on every outbound integration call.
/// Fail-closed: absence of a matching grant means the call is denied.
/// </summary>
/// <param name="AgentId">Stable agent-definition id (not a specific invocation).</param>
/// <param name="UserId">The user who granted the agent access on their behalf.</param>
/// <param name="ConnectorId">Opaque connector id from the connector registry (e.g. "sdlc-agent", "knowledge-memory", "gmail-smtp").</param>
/// <param name="GrantedAt">When the grant was recorded.</param>
/// <param name="GrantedBy">User id that authorized the grant (usually the owner; admins may grant on behalf).</param>
/// <param name="ExpiresAt">Optional expiry; null means "until revoked".</param>
public sealed record ConnectorAllowlistEntry(
    string AgentId,
    string UserId,
    string ConnectorId,
    DateTimeOffset GrantedAt,
    string GrantedBy,
    DateTimeOffset? ExpiresAt = null);
