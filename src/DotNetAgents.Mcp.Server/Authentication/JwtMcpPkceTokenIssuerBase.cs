// SPDX-License-Identifier: Apache-2.0

using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// Abstract <see cref="IMcpPkceTokenIssuer"/> that mints RFC 7519 JWTs after the framework's
/// PKCE step has succeeded. Per-service subclasses (e.g. <c>knowledge-memory serviceMcpPkceTokenIssuer</c>)
/// inherit, declare their <see cref="JwtMcpPkceTokenIssuerOptions"/>, and let this base do the
/// claim composition + signing.
/// </summary>
/// <remarks>
/// <para>Claim shape follows the umbrella story d971cd01 acceptance criteria:</para>
/// <list type="bullet">
/// <item><c>iss</c> = <see cref="JwtMcpPkceTokenIssuerOptions.Issuer"/></item>
/// <item><c>sub</c> = <see cref="McpPkceTokenIssuance.ClientId"/> (resolved CIMD URL or legacy client id)</item>
/// <item><c>aud</c> = <see cref="JwtMcpPkceTokenIssuerOptions.Audience"/></item>
/// <item><c>iat</c> / <c>exp</c> from <see cref="TimeProvider"/> + clamped TokenLifetime</item>
/// <item><c>mcp_scopes</c> = space-separated scopes returned by <see cref="DeriveScopes"/></item>
/// </list>
/// <para>
/// Tokens MUST NOT be logged. The base never writes the signed JWT to ILogger output;
/// subclasses that override behavior must observe the same rule.
/// </para>
/// </remarks>
public abstract class JwtMcpPkceTokenIssuerBase : IMcpPkceTokenIssuer
{
    private static readonly JsonWebTokenHandler _handler = new()
    {
        SetDefaultTimesOnTokenCreation = false,
        MapInboundClaims = false,
    };

    private readonly ISigningKeyProvider _keyProvider;
    private readonly IOptionsMonitor<JwtMcpPkceTokenIssuerOptions> _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<JwtMcpPkceTokenIssuerBase> _logger;

    protected JwtMcpPkceTokenIssuerBase(
        ISigningKeyProvider keyProvider,
        IOptionsMonitor<JwtMcpPkceTokenIssuerOptions> options,
        TimeProvider? clock = null,
        ILogger<JwtMcpPkceTokenIssuerBase>? logger = null)
    {
        _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _clock = clock ?? TimeProvider.System;
        _logger = logger ?? NullLogger<JwtMcpPkceTokenIssuerBase>.Instance;
    }

    public Task<McpPkceTokenResponse> IssueAsync(McpPkceTokenIssuance issuance, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(issuance);
        var opts = _options.CurrentValue;

        if (string.IsNullOrWhiteSpace(opts.Issuer))
        {
            throw new InvalidOperationException(
                $"{GetType().Name} has no configured Issuer. Bind JwtMcpPkceTokenIssuerOptions.Issuer or set DotNetAgents:Mcp:Server:Issuer:Issuer.");
        }

        if (string.IsNullOrWhiteSpace(opts.Audience))
        {
            throw new InvalidOperationException(
                $"{GetType().Name} has no configured Audience. Bind JwtMcpPkceTokenIssuerOptions.Audience.");
        }

        var lifetime = ClampLifetime(opts.TokenLifetime);
        var now = _clock.GetUtcNow();
        var expires = now + lifetime;
        var scopes = DeriveScopes(issuance);
        var allowed = ApplyScopeAllowlist(scopes, opts.ScopeAllowlist);

        var subject = new ClaimsIdentity();
        subject.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, issuance.ClientId));
        if (allowed.Count > 0)
        {
            subject.AddClaim(new Claim("mcp_scopes", string.Join(' ', allowed)));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = opts.Issuer,
            Audience = opts.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            Subject = subject,
            SigningCredentials = _keyProvider.GetCurrentSigningCredentials(),
        };

        var jwt = _handler.CreateToken(descriptor);
        var expiresIn = (int)Math.Round(lifetime.TotalSeconds, MidpointRounding.AwayFromZero);
        var scopeClaim = allowed.Count == 0 ? null : string.Join(' ', allowed);

        // NOTE: do NOT log the JWT itself. Per umbrella SecurityNotes: tokens MUST NOT be logged.
        return Task.FromResult(new McpPkceTokenResponse(
            access_token: jwt,
            token_type: "Bearer",
            expires_in: expiresIn,
            refresh_token: null,
            scope: scopeClaim));
    }

    /// <summary>
    /// Subclasses may override to derive a per-issuance scope set (e.g. read-only tokens for
    /// certain client ids). Default returns the full allowlist; this base then intersects with
    /// <see cref="JwtMcpPkceTokenIssuerOptions.ScopeAllowlist"/> to enforce the server-side gate.
    /// </summary>
    protected virtual IReadOnlyList<string> DeriveScopes(McpPkceTokenIssuance issuance)
        => _options.CurrentValue.ScopeAllowlist.ToArray();

    private static IReadOnlyList<string> ApplyScopeAllowlist(IReadOnlyList<string> requested, IList<string> allowlist)
    {
        if (allowlist is null || allowlist.Count == 0)
        {
            return Array.Empty<string>();
        }

        var allowSet = new HashSet<string>(allowlist, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>(requested.Count);
        foreach (var scope in requested)
        {
            if (string.IsNullOrWhiteSpace(scope)) continue;
            if (!allowSet.Contains(scope)) continue;
            if (!seen.Add(scope)) continue;
            result.Add(scope);
        }

        return result;
    }

    private TimeSpan ClampLifetime(TimeSpan requested)
    {
        if (requested <= TimeSpan.Zero)
        {
            _logger.LogWarning(
                "JwtMcpPkceTokenIssuerOptions.TokenLifetime was {Configured} (≤ 0); falling back to 1h default.",
                requested);
            return TimeSpan.FromHours(1);
        }

        if (requested > JwtMcpPkceTokenIssuerOptions.MaxTokenLifetime)
        {
            _logger.LogWarning(
                "JwtMcpPkceTokenIssuerOptions.TokenLifetime was {Configured}; clamped to {Max} per umbrella AC.",
                requested,
                JwtMcpPkceTokenIssuerOptions.MaxTokenLifetime);
            return JwtMcpPkceTokenIssuerOptions.MaxTokenLifetime;
        }

        return requested;
    }
}
