namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// Validator-side configuration for <see cref="JwtMcpPkceBearerValidator"/>. One instance
/// per service. Issuer + audience must match the issuer-side options; scope allowlist defines
/// what scopes the validator surfaces on the request context.
/// </summary>
public sealed class JwtMcpPkceBearerValidationOptions
{
    public const string SectionName = "DotNetAgents:Mcp:Server:Validator";

    /// <summary>The expected <c>iss</c> claim.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>The expected <c>aud</c> claim. The cross-service rejection lever.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Allowable clock skew between issuer + validator clocks. Default 2 minutes — matches
    /// JwtBearerOptions defaults but explicit so operators see it.
    /// </summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Per-token validation cache lifetime. Cached entries are also bounded by the token's
    /// own <c>exp</c> claim. Default 5 minutes; raise for read-heavy /mcp surfaces.
    /// </summary>
    public TimeSpan ValidationCacheLifetime { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Story (workflow orchestrator → credential resolver Bearer chain): trusted peer-service signing keys.
    /// When a peer DNA service calls this service over /mcp, the peer presents a JWT signed by
    /// its own per-service signing key with <c>iss=&lt;peer issuer&gt;</c> and
    /// <c>aud=&lt;this service's audience&gt;</c>. Audience binding (the cross-service rejection
    /// lever) is preserved — only callers with the right target audience get past the validator,
    /// regardless of which signing key signed them.
    /// </summary>
    /// <remarks>
    /// Each entry maps a peer issuer to its public key in PEM form. The validator combines the
    /// peer keys with the local key provider's <see cref="ISigningKeyProvider.GetValidationKeys"/>
    /// when forming <see cref="Microsoft.IdentityModel.Tokens.TokenValidationParameters.IssuerSigningKeys"/>,
    /// and the peer issuers are added to <see cref="Microsoft.IdentityModel.Tokens.TokenValidationParameters.ValidIssuers"/>.
    /// Tokens still must match the local <see cref="Audience"/>.
    /// </remarks>
    public IList<PeerSigningKey> PeerSigningKeysPem { get; set; } = new List<PeerSigningKey>();
}

/// <summary>
/// One trusted peer signing key. <see cref="Issuer"/> matches the <c>iss</c> claim minted by the
/// peer service's <see cref="JwtMcpPkceTokenIssuerOptions.Issuer"/>. <see cref="Pem"/> is the
/// public-key PEM (RSA SubjectPublicKeyInfo or PKCS1).
/// </summary>
public sealed class PeerSigningKey
{
    public string Issuer { get; set; } = string.Empty;
    public string Pem { get; set; } = string.Empty;
}
