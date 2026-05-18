using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// Outbound-side companion to <see cref="JwtMcpPkceBearerMiddleware"/>: a
/// <see cref="DelegatingHandler"/> that mints a JWT signed with this client's outbound
/// signing key and attaches it to the outbound request as <c>Authorization: Bearer ...</c>.
/// Audience is the target service the HttpClient calls (e.g. <c>credentials_agent</c> when
/// workflow orchestrator calls credential resolver's /mcp surface).
/// </summary>
/// <remarks>
/// <para>
/// The outbound signing key is supplied via <see cref="OutboundJwtBearerOptions.SigningKeyPem"/>
/// and is INTENTIONALLY DECOUPLED from the service's inbound <see cref="ISigningKeyProvider"/>.
/// Reason: a service whose inbound key is credentials-backed (loaded via <c>ICredentialsClient</c>)
/// cannot reuse that key for outbound credentials calls without creating a circular DI graph.
/// Two-key model means inbound + outbound rotate independently and each has its own audit chain.
/// </para>
/// <para>
/// Tokens are short-lived and cached per-handler. The handler refreshes when the cached token
/// has less than <see cref="OutboundJwtBearerOptions.RefreshSkew"/> remaining lifetime.
/// </para>
/// <para>
/// Tokens MUST NOT be logged. The handler never writes the signed JWT to ILogger output —
/// failure logs reference signing-key + audience metadata only.
/// </para>
/// </remarks>
public sealed class OutboundJwtBearerHandler : DelegatingHandler
{
    private static readonly JsonWebTokenHandler _handler = new()
    {
        SetDefaultTimesOnTokenCreation = false,
        MapInboundClaims = false,
    };

    private readonly IOptionsMonitor<OutboundJwtBearerOptions> _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<OutboundJwtBearerHandler> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CachedToken? _cached;
    private RSA? _signingRsa;
    private SigningCredentials? _signingCredentials;
    private string? _loadedKeyFingerprint;

    public OutboundJwtBearerHandler(
        IOptionsMonitor<OutboundJwtBearerOptions> options,
        TimeProvider? clock = null,
        ILogger<OutboundJwtBearerHandler>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _clock = clock ?? TimeProvider.System;
        _logger = logger ?? NullLogger<OutboundJwtBearerHandler>.Instance;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var opts = _options.CurrentValue;
        if (!opts.Enabled)
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        if (request.Headers.Authorization is not null)
        {
            // Caller already attached an Authorization header — never override it.
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        var token = await GetOrMintAsync(opts, cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> GetOrMintAsync(OutboundJwtBearerOptions opts, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow();
        if (_cached is { } existing && existing.RefreshAtUtc > now)
        {
            return existing.Token;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            now = _clock.GetUtcNow();
            if (_cached is { } again && again.RefreshAtUtc > now)
            {
                return again.Token;
            }

            var token = MintToken(opts, now);
            return token;
        }
        finally
        {
            _gate.Release();
        }
    }

    private string MintToken(OutboundJwtBearerOptions opts, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(opts.Issuer))
        {
            throw new InvalidOperationException("OutboundJwtBearerOptions.Issuer is required to mint outbound tokens.");
        }

        if (string.IsNullOrWhiteSpace(opts.Audience))
        {
            throw new InvalidOperationException("OutboundJwtBearerOptions.Audience is required to mint outbound tokens.");
        }

        if (string.IsNullOrWhiteSpace(opts.SigningKeyPem))
        {
            throw new InvalidOperationException(
                "OutboundJwtBearerOptions.SigningKeyPem is required. Provide an RSA private key in PEM form so the outbound handler can mint Bearer tokens. Decoupled from inbound signing keys to avoid DI cycles when the service's inbound key is credentials-backed.");
        }

        var signing = EnsureSigningCredentials(opts.SigningKeyPem);

        var lifetime = opts.TokenLifetime <= TimeSpan.Zero ? TimeSpan.FromMinutes(15) : opts.TokenLifetime;
        var skew = opts.RefreshSkew <= TimeSpan.Zero ? TimeSpan.FromMinutes(2) : opts.RefreshSkew;
        var expires = now + lifetime;

        var subject = new ClaimsIdentity();
        var sub = string.IsNullOrWhiteSpace(opts.Subject) ? opts.Issuer : opts.Subject;
        subject.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, sub));
        if (opts.Scopes is { Count: > 0 })
        {
            subject.AddClaim(new Claim("mcp_scopes", string.Join(' ', opts.Scopes)));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = opts.Issuer,
            Audience = opts.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            Subject = subject,
            SigningCredentials = signing,
        };

        var jwt = _handler.CreateToken(descriptor);
        // Refresh slightly before expiry so the next call doesn't race the validator's clock.
        _cached = new CachedToken(jwt, expires - skew);
        _logger.LogDebug(
            "Outbound bearer minted: issuer={Issuer} audience={Audience} expires={Expires}.",
            opts.Issuer, opts.Audience, expires);
        return jwt;
    }

    private SigningCredentials EnsureSigningCredentials(string pem)
    {
        var fingerprint = ComputeFingerprint(pem);
        if (_signingCredentials is not null && string.Equals(fingerprint, _loadedKeyFingerprint, StringComparison.Ordinal))
        {
            return _signingCredentials;
        }

        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        var key = new RsaSecurityKey(rsa) { KeyId = "outbound:" + fingerprint[..8] };
        var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        _signingRsa?.Dispose();
        _signingRsa = rsa;
        _signingCredentials = creds;
        _loadedKeyFingerprint = fingerprint;
        return creds;
    }

    private static string ComputeFingerprint(string pem)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(pem), hash);
        return Convert.ToHexString(hash);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _signingRsa?.Dispose();
            _gate.Dispose();
        }

        base.Dispose(disposing);
    }

    private readonly record struct CachedToken(string Token, DateTimeOffset RefreshAtUtc);
}

/// <summary>
/// Options for <see cref="OutboundJwtBearerHandler"/>. One instance per outbound HttpClient
/// (e.g. CredentialsClient wires its own with <c>Audience=credentials_agent</c>).
/// </summary>
public sealed class OutboundJwtBearerOptions
{
    /// <summary>Master switch. Default true; flip false to revert to legacy auth without a redeploy.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>The <c>iss</c> claim — the calling service's published issuer URL.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>The <c>aud</c> claim — the target service's audience (e.g. <c>credentials_agent</c>).</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Optional <c>sub</c> claim. Defaults to <see cref="Issuer"/> when unset.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Optional scope list emitted as the <c>mcp_scopes</c> space-separated claim.</summary>
    public IList<string> Scopes { get; set; } = new List<string>();

    /// <summary>Token lifetime. Default 15 minutes; clamped to a positive value at use.</summary>
    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Refresh skew — how far before <c>exp</c> the handler mints a fresh token.</summary>
    public TimeSpan RefreshSkew { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// RSA private key in PEM form (PKCS#1 or PKCS#8). The handler mints outbound tokens
    /// signed by this key. Intentionally separate from the service's inbound
    /// <see cref="ISigningKeyProvider"/> to avoid DI cycles when the inbound key is
    /// credentials-backed (the inbound chain depends on <c>ICredentialsClient</c>, which now
    /// depends on this handler — store outbound key material in config, not in credential resolver).
    /// </summary>
    public string SigningKeyPem { get; set; } = string.Empty;
}
