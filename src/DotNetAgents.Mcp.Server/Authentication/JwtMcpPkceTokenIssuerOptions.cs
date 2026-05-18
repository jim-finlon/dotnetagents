namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// Per-service issuer configuration consumed by <see cref="JwtMcpPkceTokenIssuerBase"/>.
/// One options instance is bound per service (knowledge-memory service, AiSessionPersistence, workflow orchestrator,
/// credential resolver) so each mints tokens with its own audience and scope allowlist.
/// </summary>
/// <remarks>
/// Per the umbrella story d971cd01 acceptance criteria: token TTL must be ≤ 24h, default 1h;
/// scope allowlist is enforced server-side and is never trusted from the client. The audience
/// claim is the cross-service rejection lever: a token with <c>aud=hive_mind</c> is rejected
/// by workflow orchestrator's validator because workflow orchestrator's audience is <c>workflow_service</c>.
/// </remarks>
public sealed class JwtMcpPkceTokenIssuerOptions
{
    /// <summary>Bind from <c>DotNetAgents:Mcp:Server:Issuer</c>.</summary>
    public const string SectionName = "DotNetAgents:Mcp:Server:Issuer";

    /// <summary>The <c>iss</c> claim placed on every minted token. Should be the public origin of this service.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>The <c>aud</c> claim placed on every minted token. The cross-service rejection lever.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Lifetime for each minted token. Default 1 hour; must be ≤ 24 hours per the umbrella AC.
    /// Values outside that range are clamped at issuance time (logged as a warning).
    /// </summary>
    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Scope allowlist for this service. <see cref="JwtMcpPkceTokenIssuerBase.IssueAsync"/>
    /// places exactly these scopes on the minted token's <c>mcp_scopes</c> claim. Subclasses
    /// may filter further per-issuance via <see cref="JwtMcpPkceTokenIssuerBase.DeriveScopes"/>.
    /// </summary>
    public IList<string> ScopeAllowlist { get; set; } = new List<string>();

    /// <summary>Maximum allowed token TTL. Constant — not operator-tunable.</summary>
    public static readonly TimeSpan MaxTokenLifetime = TimeSpan.FromHours(24);
}
