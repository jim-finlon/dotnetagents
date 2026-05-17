namespace DotNetAgents.Governance.Identity;

/// <summary>
/// Identity + authorization context of the user/actor currently invoking an agent.
/// Propagated across MCP, HTTP, and DB boundaries so downstream calls honor the INVOKER's
/// ACLs rather than the agent creator's service-account scope.
/// </summary>
/// <param name="UserId">Stable id of the invoking user (may be an actor id for non-human invokers).</param>
/// <param name="ActorType">Actor classification: "User", "Agent", "Service", "WorkstationSession", etc.</param>
/// <param name="Scopes">Claims/permissions the invoker carries. Downstream filters use these to scope reads and gate writes.</param>
/// <param name="TenantId">Optional tenant id for multi-tenant deployments.</param>
/// <param name="ImpersonatedBy">Populated when an admin impersonates another user; carries the original admin identity for audit.</param>
public sealed record InvokerContext(
    string UserId,
    string ActorType,
    IReadOnlyList<string> Scopes,
    string? TenantId = null,
    string? ImpersonatedBy = null)
{
    public bool HasScope(string scope) => Scopes.Contains(scope, StringComparer.Ordinal);

    public bool HasAllScopes(params string[] required) =>
        required.All(r => Scopes.Contains(r, StringComparer.Ordinal));
}
