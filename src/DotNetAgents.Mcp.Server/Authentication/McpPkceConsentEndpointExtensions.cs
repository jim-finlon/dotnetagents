using System.Security.Cryptography;
using DotNetAgents.Mcp.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// Story 1095b26a. Replaces the legacy <c>authorization_not_supported</c>
/// short-circuit on GET <c>/.mcp/oauth/authorize</c> with a real consent UI:
/// an HTML page rendered for valid PKCE requests + a POST decision handler
/// that mints a single-use authorization code via the existing
/// <see cref="IPkceChallengeStore"/> and 302-redirects per RFC 6749.
/// </summary>
/// <remarks>
/// <para>The token-exchange half (POST <c>/.mcp/oauth/token</c>) was already
/// shipped under story <c>d971cd01</c> + sub-slices and consumes the
/// authorization codes minted here without modification — the
/// <see cref="PkceChallengeRecord"/> contract is shared between the
/// short-lived auth-code store and the token endpoint.</para>
/// <para>Per-actor consent persistence uses <see cref="IPkceConsentStore"/>;
/// in-memory default is registered automatically by
/// <see cref="McpAuthServerExtensions.AddMcpAuthServer"/>. DB-backed
/// implementations land in a follow-up story for multi-replica deployments.</para>
/// </remarks>
public static class McpPkceConsentEndpointExtensions
{
    /// <summary>Where the operator's POSTed Allow / Deny landing handler lives.</summary>
    public const string DecisionPath = "/.mcp/oauth/authorize/decision";

    /// <summary>Where the operator audit JSON lives. Razor UI is a follow-up story.</summary>
    public const string AdminConsentsPath = "/.mcp/admin/oauth/consents";

    /// <summary>Authorization codes are single-use and short-lived per RFC 7636 §4.4.</summary>
    public const int AuthorizationCodeTtlSeconds = 60;

    /// <summary>
    /// Map the consent-UI endpoints. Call after <see cref="McpAuthServerExtensions.MapMcpAuth"/>
    /// to *replace* the legacy short-circuit with the real consent flow.
    /// </summary>
    public static IEndpointRouteBuilder MapMcpPkceConsentEndpoints(
        this IEndpointRouteBuilder endpoints,
        string serviceName)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var hosting = endpoints.ServiceProvider.GetRequiredService<IOptions<McpAuthHostingOptions>>().Value;
        var authorizePath = string.IsNullOrWhiteSpace(hosting.AuthorizationEndpointPath)
            ? new McpAuthHostingOptions().AuthorizationEndpointPath
            : hosting.AuthorizationEndpointPath;

        // GET /.mcp/oauth/authorize — render consent page or skip with persisted decision.
        endpoints.MapGet(authorizePath, async (HttpContext http) =>
        {
            var query = http.Request.Query;
            var clientId = query["client_id"].ToString();
            var responseType = query["response_type"].ToString();
            var redirectUri = query["redirect_uri"].ToString();
            var codeChallenge = query["code_challenge"].ToString();
            var codeChallengeMethod = query["code_challenge_method"].ToString();
            var scope = query["scope"].ToString();
            var state = query["state"].ToString();
            var prompt = query["prompt"].ToString();
            var actorId = ResolveActorId(http);

            var validationError = ValidatePkceParams(responseType, clientId, redirectUri, codeChallenge, codeChallengeMethod);
            if (validationError is not null)
                return Results.Json(validationError, statusCode: StatusCodes.Status400BadRequest);

            var requestedScopes = ParseScopes(scope);
            var consentStore = http.RequestServices.GetRequiredService<IPkceConsentStore>();

            // prompt=consent forces a fresh prompt; otherwise look for a persisted Allow.
            var skipPromptOk = !string.Equals(prompt, "consent", StringComparison.OrdinalIgnoreCase);
            if (skipPromptOk)
            {
                var existing = await consentStore.FindCoveringAsync(actorId, clientId, requestedScopes, http.RequestAborted).ConfigureAwait(false);
                if (existing is not null && existing.Decision == PkceConsentDecision.Allow)
                {
                    var code = await MintAuthorizationCodeAsync(http, clientId, codeChallenge, codeChallengeMethod);
                    return Results.Redirect(BuildRedirectUri(redirectUri, code, state), permanent: false);
                }
            }

            var model = new PkceConsentPageModel(
                ClientId: clientId,
                ClientDisplayName: clientId,
                ServiceName: serviceName,
                ActorId: actorId,
                RedirectUri: redirectUri,
                CodeChallenge: codeChallenge,
                CodeChallengeMethod: codeChallengeMethod,
                RequestedScopes: requestedScopes,
                DecisionPostPath: DecisionPath,
                State: state,
                AuthorizationCodeTtlSeconds: AuthorizationCodeTtlSeconds);

            return Results.Content(PkceConsentPageRenderer.Render(model), contentType: "text/html; charset=utf-8");
        })
        .DisableAntiforgery()
        .WithName("McpPkceConsentAuthorize")
        .WithTags("MCP", "PKCE-Consent");

        // POST /.mcp/oauth/authorize/decision — operator submits Allow / Deny.
        endpoints.MapPost(DecisionPath, async (HttpContext http) =>
        {
            if (!http.Request.HasFormContentType)
                return Results.BadRequest(new { error = "invalid_request", error_description = "Decision endpoint requires form-encoded body." });

            var form = await http.Request.ReadFormAsync(http.RequestAborted).ConfigureAwait(false);
            var decision = form["decision"].ToString();
            var clientId = form["client_id"].ToString();
            var redirectUri = form["redirect_uri"].ToString();
            var codeChallenge = form["code_challenge"].ToString();
            var codeChallengeMethod = form["code_challenge_method"].ToString();
            var scope = form["scope"].ToString();
            var state = form["state"].ToString();
            var actorIdForm = form["actor_id"].ToString();
            var actorId = string.IsNullOrWhiteSpace(actorIdForm) ? ResolveActorId(http) : actorIdForm;

            if (string.IsNullOrWhiteSpace(decision)
                || (!string.Equals(decision, "allow", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(decision, "deny", StringComparison.OrdinalIgnoreCase)))
            {
                return Results.BadRequest(new { error = "invalid_request", error_description = "Missing or invalid 'decision' (allow|deny)." });
            }

            var validationError = ValidatePkceParams("code", clientId, redirectUri, codeChallenge, codeChallengeMethod);
            if (validationError is not null)
                return Results.BadRequest(validationError);

            var requestedScopes = ParseScopes(scope);
            var consentStore = http.RequestServices.GetRequiredService<IPkceConsentStore>();

            if (string.Equals(decision, "deny", StringComparison.OrdinalIgnoreCase))
            {
                await consentStore.RecordAsync(new PkceConsentRecord(
                    Id: Guid.NewGuid(),
                    ActorId: actorId,
                    ClientId: clientId,
                    Scopes: requestedScopes,
                    Decision: PkceConsentDecision.Deny,
                    GrantedAtUtc: DateTimeOffset.UtcNow), http.RequestAborted);

                // Per RFC 6749 §4.1.2.1 access_denied is the canonical denial signal.
                return Results.Redirect(BuildErrorRedirectUri(redirectUri, "access_denied", state), permanent: false);
            }

            await consentStore.RecordAsync(new PkceConsentRecord(
                Id: Guid.NewGuid(),
                ActorId: actorId,
                ClientId: clientId,
                Scopes: requestedScopes,
                Decision: PkceConsentDecision.Allow,
                GrantedAtUtc: DateTimeOffset.UtcNow), http.RequestAborted);

            var code = await MintAuthorizationCodeAsync(http, clientId, codeChallenge, codeChallengeMethod);
            return Results.Redirect(BuildRedirectUri(redirectUri, code, state), permanent: false);
        })
        .DisableAntiforgery()
        .WithName("McpPkceConsentDecision")
        .WithTags("MCP", "PKCE-Consent");

        // GET /.mcp/admin/oauth/consents — operator audit JSON. Razor UI in a follow-up.
        endpoints.MapGet(AdminConsentsPath, async (HttpContext http) =>
        {
            var consentStore = http.RequestServices.GetRequiredService<IPkceConsentStore>();
            var actorFilter = http.Request.Query["actorId"].ToString();
            var consents = await consentStore.ListAsync(
                actorIdFilter: string.IsNullOrWhiteSpace(actorFilter) ? null : actorFilter,
                cancellationToken: http.RequestAborted).ConfigureAwait(false);

            return Results.Json(consents.Select(c => new
            {
                id = c.Id,
                actorId = c.ActorId,
                clientId = c.ClientId,
                scopes = c.Scopes,
                decision = c.Decision.ToString(),
                grantedAtUtc = c.GrantedAtUtc,
                expiresAtUtc = c.ExpiresAtUtc,
            }).ToList());
        })
        .WithName("McpPkceAdminConsents")
        .WithTags("MCP", "PKCE-Consent", "Admin");

        // DELETE /.mcp/admin/oauth/consents/{id} — operator revoke.
        endpoints.MapDelete($"{AdminConsentsPath}/{{id:guid}}", async (Guid id, HttpContext http) =>
        {
            var consentStore = http.RequestServices.GetRequiredService<IPkceConsentStore>();
            await consentStore.RevokeAsync(id, http.RequestAborted).ConfigureAwait(false);
            return Results.NoContent();
        })
        .WithName("McpPkceAdminConsentRevoke")
        .WithTags("MCP", "PKCE-Consent", "Admin");

        return endpoints;
    }

    /// <summary>
    /// Cryptographically random, URL-safe authorization code. 256 bits of entropy.
    /// </summary>
    internal static string NewAuthorizationCode()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        // base64-url without padding.
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static async Task<string> MintAuthorizationCodeAsync(
        HttpContext http,
        string clientId,
        string codeChallenge,
        string codeChallengeMethod)
    {
        var code = NewAuthorizationCode();
        var store = http.RequestServices.GetRequiredService<IPkceChallengeStore>();
        await store.StoreAsync(code, new PkceChallengeRecord(
            CodeChallenge: codeChallenge,
            CodeChallengeMethod: codeChallengeMethod,
            ClientId: clientId,
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddSeconds(AuthorizationCodeTtlSeconds)), http.RequestAborted).ConfigureAwait(false);
        return code;
    }

    private static IReadOnlyList<string> ParseScopes(string? rawScope)
    {
        if (string.IsNullOrWhiteSpace(rawScope)) return Array.Empty<string>();
        return rawScope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string ResolveActorId(HttpContext http)
    {
        // Prefer an authenticated identity if the host has middleware that populates it.
        var name = http.User?.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(name)) return name!;

        // Workstation operators today are driven by an X-Actor-Id hint when present.
        var actorHeader = http.Request.Headers["X-Actor-Id"].ToString();
        if (!string.IsNullOrWhiteSpace(actorHeader)) return actorHeader;

        return "anonymous";
    }

    private static object? ValidatePkceParams(
        string responseType,
        string clientId,
        string redirectUri,
        string codeChallenge,
        string codeChallengeMethod)
    {
        if (!string.Equals(responseType, "code", StringComparison.Ordinal))
            return new { error = "unsupported_response_type", error_description = "Only response_type=code is supported." };
        if (string.IsNullOrWhiteSpace(clientId))
            return new { error = "invalid_request", error_description = "Missing 'client_id'." };
        if (string.IsNullOrWhiteSpace(redirectUri))
            return new { error = "invalid_request", error_description = "Missing 'redirect_uri'." };
        if (string.IsNullOrWhiteSpace(codeChallenge))
            return new { error = "invalid_request", error_description = "Missing 'code_challenge'; PKCE is mandatory." };
        if (!string.Equals(codeChallengeMethod, "S256", StringComparison.Ordinal))
            return new { error = "invalid_request", error_description = "Only S256 code_challenge_method is supported." };
        return null;
    }

    private static string BuildRedirectUri(string redirectUri, string code, string? state)
    {
        var separator = redirectUri.Contains('?') ? '&' : '?';
        var sb = new System.Text.StringBuilder(redirectUri);
        sb.Append(separator).Append("code=").Append(Uri.EscapeDataString(code));
        if (!string.IsNullOrWhiteSpace(state))
            sb.Append("&state=").Append(Uri.EscapeDataString(state));
        return sb.ToString();
    }

    private static string BuildErrorRedirectUri(string redirectUri, string error, string? state)
    {
        var separator = redirectUri.Contains('?') ? '&' : '?';
        var sb = new System.Text.StringBuilder(redirectUri);
        sb.Append(separator).Append("error=").Append(Uri.EscapeDataString(error));
        if (!string.IsNullOrWhiteSpace(state))
            sb.Append("&state=").Append(Uri.EscapeDataString(state));
        return sb.ToString();
    }
}
