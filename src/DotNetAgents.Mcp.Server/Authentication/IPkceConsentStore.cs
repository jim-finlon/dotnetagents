// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// Story 1095b26a. Per-actor consent store for the PKCE consent UI. Distinct
/// from <see cref="IPkceChallengeStore"/> (which is the short-lived
/// authorization-code → code-challenge map): consent records persist across
/// many auth codes so re-prompts only fire on (a) scope expansion, (b)
/// operator-revoked consent, or (c) deliberate <c>prompt=consent</c> on the
/// authorize URL.
/// </summary>
/// <remarks>
/// <para>The default in-memory store is fine for lab deployments. Core 4
/// services that need restart-surviving consent decisions should opt into a
/// durable implementation so a consent recorded before redeploy is still
/// visible after process restart.</para>
/// </remarks>
public interface IPkceConsentStore
{
    /// <summary>
    /// Record an Allow / Deny decision. If a record already exists for the
    /// (actor, client, service) tuple, replace it; active consent decisions are
    /// last-write-wins.
    /// </summary>
    Task RecordAsync(PkceConsentRecord decision, CancellationToken cancellationToken = default);

    /// <summary>
    /// Look up an existing decision that covers the requested scopes. Returns
    /// null when no record exists, the record is expired, or the existing
    /// scopes don't cover the request (caller should re-prompt for the
    /// expanded scope set).
    /// </summary>
    Task<PkceConsentRecord?> FindCoveringAsync(
        string actorId,
        string clientId,
        IReadOnlyCollection<string> requestedScopes,
        CancellationToken cancellationToken = default,
        string? serviceName = null);

    /// <summary>List consent records for the operator audit surface.</summary>
    Task<IReadOnlyList<PkceConsentRecord>> ListAsync(
        string? actorIdFilter = null,
        bool includeRevoked = false,
        CancellationToken cancellationToken = default);

    /// <summary>Revoke a single consent by id. Idempotent; missing records are a no-op.</summary>
    Task RevokeAsync(Guid consentId, CancellationToken cancellationToken = default);
}

/// <summary>One persisted Allow/Deny decision for the PKCE consent UI.</summary>
/// <param name="Id">Stable identifier for revoke + audit.</param>
/// <param name="ActorId">The operator (or actor) that made the decision.</param>
/// <param name="ClientId">CIMD-resolved or legacy client id the consent applies to.</param>
/// <param name="ServiceName">The MCP service this consent applies to. Same actor + client consent does not cross service boundaries.</param>
/// <param name="Scopes">Scopes the operator authorized at decision time. Future authorize requests subset of this list skip the prompt; superset triggers re-prompt.</param>
/// <param name="Decision">Allow or Deny.</param>
/// <param name="GrantedAtUtc">When the decision was made.</param>
/// <param name="ExpiresAtUtc">When the decision expires; null = no expiry (operator must explicitly revoke).</param>
/// <param name="RevokedAtUtc">When an operator revoked this record; null means active until expiry.</param>
public sealed record PkceConsentRecord(
    Guid Id,
    string ActorId,
    string ClientId,
    IReadOnlyList<string> Scopes,
    PkceConsentDecision Decision,
    DateTimeOffset GrantedAtUtc,
    DateTimeOffset? ExpiresAtUtc = null,
    string ServiceName = "",
    DateTimeOffset? RevokedAtUtc = null);

public enum PkceConsentDecision
{
    Allow = 0,
    Deny = 1,
}
