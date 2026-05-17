using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// Validates JWTs minted by <see cref="JwtMcpPkceTokenIssuerBase"/>. Per the umbrella story AC,
/// audience binding is the cross-service rejection lever: a token with <c>aud=hive_mind</c> is
/// rejected here when this validator is configured for <c>aud=workflow_service</c>.
/// </summary>
/// <remarks>
/// <para>
/// Validation results are cached per-token (keyed on SHA-256 of the token bytes) so /mcp request
/// overhead stays at the AC's ~1ms budget. Cache entries expire at <c>min(token.exp, options.ValidationCacheLifetime)</c>,
/// and the entire cache is cleared when <see cref="ISigningKeyProvider.KeyRotated"/> fires.
/// </para>
/// <para>
/// Tokens MUST NOT be logged. The validator never writes the raw JWT to ILogger output —
/// failure logs reference the failure reason and the cache key only.
/// </para>
/// </remarks>
public sealed class JwtMcpPkceBearerValidator
{
    private static readonly JsonWebTokenHandler _handler = new()
    {
        MapInboundClaims = false,
    };

    private readonly ISigningKeyProvider _keyProvider;
    private readonly IOptionsMonitor<JwtMcpPkceBearerValidationOptions> _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<JwtMcpPkceBearerValidator> _logger;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.Ordinal);

    public JwtMcpPkceBearerValidator(
        ISigningKeyProvider keyProvider,
        IOptionsMonitor<JwtMcpPkceBearerValidationOptions> options,
        TimeProvider? clock = null,
        ILogger<JwtMcpPkceBearerValidator>? logger = null)
    {
        _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _clock = clock ?? TimeProvider.System;
        _logger = logger ?? NullLogger<JwtMcpPkceBearerValidator>.Instance;
        _keyProvider.KeyRotated += OnKeyRotated;
    }

    /// <summary>
    /// Validate <paramref name="bearerToken"/> against this service's configured issuer + audience.
    /// Returns an <see cref="JwtMcpPkceBearerValidationResult"/> capturing success or the failure reason.
    /// </summary>
    public async Task<JwtMcpPkceBearerValidationResult> ValidateAsync(string bearerToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            return JwtMcpPkceBearerValidationResult.Fail("missing_bearer_token");
        }

        try
        {
            return await ValidateCoreAsync(bearerToken, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Honor cancellation as-is — the request was cancelled, not the auth state.
            throw;
        }
        catch (Exception ex)
        {
            // Story b17b5dda — any unhandled exception below (e.g. no-signing-keys
            // when the service does not issue its own tokens, or peer-key import
            // failure) MUST surface as RFC 6750 invalid_token rather than HTTP
            // 500. The middleware writes 401 + WWW-Authenticate from a Fail
            // result; without this wrap the exception bubbles up to ASP.NET and
            // OAuth clients can't tell auth-failure from outage.
            _logger.LogWarning(ex, "JWT validator raised an exception; mapping to invalid_token per RFC 6750.");
            return JwtMcpPkceBearerValidationResult.Fail("validator_error");
        }
    }

    private async Task<JwtMcpPkceBearerValidationResult> ValidateCoreAsync(string bearerToken, CancellationToken cancellationToken)
    {
        var cacheKey = ComputeCacheKey(bearerToken);
        var now = _clock.GetUtcNow();
        if (_cache.TryGetValue(cacheKey, out var existing))
        {
            if (existing.ExpiresAtUtc > now)
            {
                return existing.Result;
            }

            _cache.TryRemove(cacheKey, out _);
        }

        var opts = _options.CurrentValue;
        var (issuerSigningKeys, validIssuers) = ComposeTrustMaterial(opts);
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = opts.Issuer,
            ValidIssuers = validIssuers,
            ValidateIssuer = !string.IsNullOrEmpty(opts.Issuer) || (validIssuers is { Count: > 0 }),
            ValidAudience = opts.Audience,
            ValidateAudience = !string.IsNullOrEmpty(opts.Audience),
            IssuerSigningKeys = issuerSigningKeys,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = opts.ClockSkew,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            // The handler's default lifetime validator uses DateTime.UtcNow directly which makes the
            // validator untestable with an injected TimeProvider. Plumb the clock in explicitly so
            // unit tests with a FakeTimeProvider can drive expiry deterministically.
            LifetimeValidator = (notBefore, expires, _, vp) =>
            {
                var nowUtc = _clock.GetUtcNow().UtcDateTime;
                if (notBefore.HasValue && notBefore.Value > nowUtc + vp.ClockSkew)
                {
                    throw new SecurityTokenNotYetValidException($"Token not valid until {notBefore:O}; clock at {nowUtc:O}.");
                }

                if (expires.HasValue && expires.Value < nowUtc - vp.ClockSkew)
                {
                    throw new SecurityTokenExpiredException($"Token expired at {expires:O}; clock at {nowUtc:O}.");
                }

                return true;
            },
        };

        var validation = await _handler.ValidateTokenAsync(bearerToken, parameters).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            var reason = ResolveFailureReason(validation.Exception);
            _logger.LogDebug("JWT validation failed (cache_key={CacheKey}, reason={Reason}).", cacheKey, reason);
            return JwtMcpPkceBearerValidationResult.Fail(reason);
        }

        var identity = (ClaimsIdentity)validation.ClaimsIdentity;
        var principal = new ClaimsPrincipal(identity);
        var scopes = ParseScopes(identity);
        var expiresAt = ResolveExpiry(identity, now);
        var result = JwtMcpPkceBearerValidationResult.Succeed(principal, scopes, expiresAt);

        var cacheTtl = TimeSpan.FromTicks(Math.Min(opts.ValidationCacheLifetime.Ticks, Math.Max(TimeSpan.Zero.Ticks, (expiresAt - now).Ticks)));
        if (cacheTtl > TimeSpan.Zero)
        {
            _cache[cacheKey] = new CacheEntry(result, now + cacheTtl);
        }

        return result;
    }

    /// <summary>Clear all cached validations. Called automatically on signing-key rotation.</summary>
    public void ClearCache() => _cache.Clear();

    /// <summary>
    /// Combine the local signing keys with any configured peer keys + collect the union of
    /// trusted issuers. Story (WorkflowService → CredentialsAgent Bearer chain): peer entries enable
    /// service-to-service auth without sharing private key material. Audience binding still
    /// rejects cross-target replays.
    /// </summary>
    private (IList<SecurityKey> SigningKeys, IList<string> Issuers) ComposeTrustMaterial(JwtMcpPkceBearerValidationOptions opts)
    {
        // Try to read local keys; if none configured (e.g. credentials-only-as-validator deploy)
        // fall back to whatever peer keys exist. A throwing key provider means "no local keys".
        IReadOnlyList<SecurityKey> localKeys;
        try
        {
            localKeys = _keyProvider.GetValidationKeys();
        }
        catch (Exception ex)
        {
            // Local key provider isn't ready (or isn't configured). Continue with peers-only
            // when peers are configured; otherwise re-throw so the original "no signing key"
            // error surfaces to the caller as before.
            if (opts.PeerSigningKeysPem is null || opts.PeerSigningKeysPem.Count == 0)
            {
                throw;
            }

            _logger.LogWarning(ex, "Local signing key provider failed; continuing with peer-only trust ({PeerCount} peers).", opts.PeerSigningKeysPem.Count);
            localKeys = Array.Empty<SecurityKey>();
        }

        if (opts.PeerSigningKeysPem is null || opts.PeerSigningKeysPem.Count == 0)
        {
            // Hot path: no peer trust configured. Defaults to the local-only behavior.
            return (localKeys.ToList(), Array.Empty<string>());
        }

        var combinedKeys = new List<SecurityKey>(localKeys);
        var issuers = new List<string>();
        if (!string.IsNullOrWhiteSpace(opts.Issuer))
        {
            issuers.Add(opts.Issuer);
        }

        foreach (var peer in opts.PeerSigningKeysPem)
        {
            if (peer is null || string.IsNullOrWhiteSpace(peer.Issuer) || string.IsNullOrWhiteSpace(peer.Pem))
            {
                continue;
            }

            try
            {
                var rsa = RSA.Create();
                rsa.ImportFromPem(peer.Pem);
                var keyId = ComputePeerKeyId(peer.Pem);
                combinedKeys.Add(new RsaSecurityKey(rsa) { KeyId = keyId });
                if (!issuers.Contains(peer.Issuer, StringComparer.Ordinal))
                {
                    issuers.Add(peer.Issuer);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to import peer signing key for issuer={Issuer}; this peer will not be trusted until config is fixed.",
                    peer.Issuer);
            }
        }

        return (combinedKeys, issuers);
    }

    private static string ComputePeerKeyId(string pem)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(pem), hash);
        return "peer:" + Convert.ToHexString(hash[..8]);
    }

    private void OnKeyRotated(object? sender, EventArgs e)
    {
        _logger.LogInformation("Signing key rotated; clearing JWT validation cache.");
        ClearCache();
    }

    private static string ComputeCacheKey(string token)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(token), hash);
        return Convert.ToHexString(hash);
    }

    private static IReadOnlyList<string> ParseScopes(ClaimsIdentity identity)
    {
        var raw = identity.FindFirst("mcp_scopes")?.Value;
        if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<string>();
        return raw.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static DateTimeOffset ResolveExpiry(ClaimsIdentity identity, DateTimeOffset fallback)
    {
        var expClaim = identity.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
        if (long.TryParse(expClaim, out var unix))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unix);
        }

        return fallback;
    }

    private static string ResolveFailureReason(Exception? ex)
    {
        return ex switch
        {
            SecurityTokenExpiredException => "token_expired",
            SecurityTokenInvalidAudienceException => "invalid_audience",
            SecurityTokenInvalidIssuerException => "invalid_issuer",
            SecurityTokenSignatureKeyNotFoundException => "signature_key_not_found",
            SecurityTokenInvalidSignatureException => "invalid_signature",
            SecurityTokenNotYetValidException => "not_yet_valid",
            SecurityTokenMalformedException => "malformed_token",
            _ => "validation_failed",
        };
    }

    private readonly record struct CacheEntry(JwtMcpPkceBearerValidationResult Result, DateTimeOffset ExpiresAtUtc);
}

/// <summary>Outcome of a JWT validation attempt.</summary>
public sealed class JwtMcpPkceBearerValidationResult
{
    private JwtMcpPkceBearerValidationResult(bool isValid, ClaimsPrincipal? principal, IReadOnlyList<string> scopes, DateTimeOffset expiresAtUtc, string? failureReason)
    {
        IsValid = isValid;
        Principal = principal;
        Scopes = scopes;
        ExpiresAtUtc = expiresAtUtc;
        FailureReason = failureReason;
    }

    public bool IsValid { get; }
    public ClaimsPrincipal? Principal { get; }
    public IReadOnlyList<string> Scopes { get; }
    public DateTimeOffset ExpiresAtUtc { get; }
    public string? FailureReason { get; }

    public static JwtMcpPkceBearerValidationResult Succeed(ClaimsPrincipal principal, IReadOnlyList<string> scopes, DateTimeOffset expiresAtUtc)
        => new(true, principal, scopes, expiresAtUtc, null);

    public static JwtMcpPkceBearerValidationResult Fail(string reason)
        => new(false, null, Array.Empty<string>(), DateTimeOffset.MinValue, reason);
}
