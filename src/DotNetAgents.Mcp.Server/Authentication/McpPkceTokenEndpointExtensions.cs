// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Mcp.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// Maps the OAuth 2.0 token endpoint with PKCE-mandatory enforcement per the November 2025 MCP
/// spec. The endpoint accepts <c>application/x-www-form-urlencoded</c> bodies with
/// <c>grant_type=authorization_code</c>, <c>code</c>, <c>code_verifier</c>, and <c>client_id</c>.
/// </summary>
/// <remarks>
/// <para>
/// This adapter does NOT mint real access tokens — that is the host service's job. It owns the
/// PKCE verification step and the canned error responses defined by RFC 6749 / RFC 7636. Hosts
/// chain their own token-issuance code via <see cref="IMcpPkceTokenIssuer"/>.
/// </para>
/// <para>
/// In <see cref="McpAuthMode.Strict"/> mode missing or invalid PKCE returns a 400 with
/// <c>invalid_grant</c>. In <see cref="McpAuthMode.Both"/> mode the same applies — legacy
/// clients use Bearer auth on the JSON-RPC <c>/mcp</c> endpoint instead of the token-exchange.
/// In <see cref="McpAuthMode.Legacy"/> mode the endpoint returns 404 (not exposed).
/// </para>
/// </remarks>
public static class McpPkceTokenEndpointExtensions
{
    /// <summary>
    /// Maps POST <see cref="McpAuthHostingOptions.TokenEndpointPath"/>. Caller registers an
    /// <see cref="IMcpPkceTokenIssuer"/> via DI which mints the access token after successful
    /// PKCE verification.
    /// </summary>
    public static IEndpointRouteBuilder MapMcpPkceTokenEndpoint(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<McpAuthHostingOptions>>().Value;
        var path = string.IsNullOrWhiteSpace(options.TokenEndpointPath)
            ? new McpAuthHostingOptions().TokenEndpointPath
            : options.TokenEndpointPath;

        endpoints.MapPost(path, (Delegate)HandleAsync)
            .DisableAntiforgery()
            .WithName("McpPkceTokenEndpoint")
            .WithTags("MCP", "OAuth");

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(HttpContext http)
    {
        var hosting = http.RequestServices.GetRequiredService<IOptions<McpAuthHostingOptions>>().Value;
        if (hosting.Mode == McpAuthMode.Legacy)
        {
            return Results.NotFound();
        }

        if (!http.Request.HasFormContentType)
        {
            return TokenError("invalid_request", "Token endpoint requires application/x-www-form-urlencoded body.");
        }

        var form = await http.Request.ReadFormAsync(http.RequestAborted).ConfigureAwait(false);
        var grantType = form["grant_type"].ToString();
        var code = form["code"].ToString();
        var verifier = form["code_verifier"].ToString();
        var clientId = form["client_id"].ToString();

        if (!string.Equals(grantType, "authorization_code", StringComparison.Ordinal))
        {
            return TokenError("unsupported_grant_type", $"Only authorization_code is supported; got '{grantType}'.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return TokenError("invalid_request", "Missing 'code'.");
        }

        if (string.IsNullOrWhiteSpace(verifier))
        {
            return TokenError("invalid_grant", "Missing 'code_verifier'; PKCE is mandatory in MCP November 2025.");
        }

        if (string.IsNullOrWhiteSpace(clientId))
        {
            return TokenError("invalid_request", "Missing 'client_id'.");
        }

        var store = http.RequestServices.GetRequiredService<IPkceChallengeStore>();
        // Story 9390068a — wrap every post-form step in try/catch so server-side
        // failures (transient signing-key fetch, store contention, issuer faults)
        // surface as structured RFC 6749 envelopes instead of empty-body Kestrel
        // 500s. Spec-compliant MCP clients (codex/claude/cursor) need to
        // distinguish "service is misconfigured" from "service is unreachable"
        // from "token is bad" — RFC 6749 §5.2 mandates a JSON body for the
        // failure case.
        PkceChallengeRecord? record;
        try
        {
            record = await store.ConsumeAsync(code, http.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogTokenEndpointFault(http, ex, "consume_challenge");
            return ServerError("Authorization code lookup failed.");
        }

        if (record is null)
        {
            return TokenError("invalid_grant", "Authorization code unknown or expired.");
        }

        if (!string.Equals(record.ClientId, clientId, StringComparison.Ordinal))
        {
            return TokenError("invalid_grant", "client_id does not match the issued authorization code.");
        }

        bool pkceOk;
        try
        {
            pkceOk = PkceVerifier.Verify(verifier, record.CodeChallenge, record.CodeChallengeMethod);
        }
        catch (Exception ex)
        {
            LogTokenEndpointFault(http, ex, "pkce_verify");
            return ServerError("PKCE verification raised an unexpected fault.");
        }

        if (!pkceOk)
        {
            return TokenError("invalid_grant", "PKCE code_verifier did not match the stored code_challenge.");
        }

        var issuer = http.RequestServices.GetService<IMcpPkceTokenIssuer>() ?? new DefaultMcpPkceTokenIssuer();
        try
        {
            var token = await issuer.IssueAsync(new McpPkceTokenIssuance(clientId, record), http.RequestAborted).ConfigureAwait(false);
            return Results.Json(token);
        }
        catch (Exception ex)
        {
            LogTokenEndpointFault(http, ex, "token_issuance");
            return ServerError("Token issuance failed for this authorization code.");
        }
    }

    private static IResult TokenError(string error, string description)
    {
        return Results.Json(new
        {
            error,
            error_description = description,
        }, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Story 9390068a — RFC 6749 §5.2 <c>server_error</c> envelope returned with
    /// HTTP 500 so clients can parse a JSON body instead of receiving an empty
    /// Kestrel page. We deliberately do NOT leak the underlying exception type
    /// or signing-key state into <c>error_description</c>; operators get the
    /// detail through the structured log at the call site.
    /// </summary>
    private static IResult ServerError(string description)
    {
        return Results.Json(new
        {
            error = "server_error",
            error_description = description,
        }, statusCode: StatusCodes.Status500InternalServerError);
    }

    private static void LogTokenEndpointFault(HttpContext http, Exception ex, string phase)
    {
        var loggerFactory = http.RequestServices.GetService<Microsoft.Extensions.Logging.ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger("DotNetAgents.Mcp.Server.Authentication.McpPkceTokenEndpoint");
        logger?.LogError(ex, "MCP PKCE token endpoint faulted during {Phase}.", phase);
    }
}

/// <summary>Token issuer the MCP server delegates to after PKCE has verified.</summary>
public interface IMcpPkceTokenIssuer
{
    Task<McpPkceTokenResponse> IssueAsync(McpPkceTokenIssuance issuance, CancellationToken cancellationToken);
}

/// <param name="ClientId">Resolved CIMD URL or legacy client id.</param>
/// <param name="Challenge">The PKCE record consumed for this exchange. Issuer can stamp it on the token claim set.</param>
public sealed record McpPkceTokenIssuance(string ClientId, PkceChallengeRecord Challenge);

/// <summary>RFC 6749 §5.1 token response shape.</summary>
public sealed record McpPkceTokenResponse(
    string access_token,
    string token_type = "Bearer",
    int expires_in = 3600,
    string? refresh_token = null,
    string? scope = null);

/// <summary>Default no-op issuer that returns a placeholder token so the endpoint completes without an explicit issuer registration. Hosts SHOULD replace this with a real token-issuance service.</summary>
public sealed class DefaultMcpPkceTokenIssuer : IMcpPkceTokenIssuer
{
    public Task<McpPkceTokenResponse> IssueAsync(McpPkceTokenIssuance issuance, CancellationToken cancellationToken)
        => Task.FromResult(new McpPkceTokenResponse(
            access_token: $"dna-mcp-placeholder-{Guid.NewGuid():N}",
            token_type: "Bearer",
            expires_in: 3600,
            scope: null));
}
