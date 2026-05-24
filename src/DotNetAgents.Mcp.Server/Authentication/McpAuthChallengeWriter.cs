// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Mcp.Models;
using Microsoft.AspNetCore.Http;

namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// Shared helpers for the X-Api-Key auth path on Core 4 services. Centralizes:
/// <list type="bullet">
/// <item>The canonical list of MCP discovery paths that <em>must</em> bypass api-key auth so
///   <c>/.well-known/oauth-protected-resource</c> and <c>/.well-known/oauth-authorization-server</c>
///   stay reachable for unauthenticated MCP clients (RFC 9728 / RFC 8414).</item>
/// <item>A standardized 401 writer that emits <c>WWW-Authenticate</c> per RFC 7235 so MCP clients
///   render "API key missing" instead of falling back to a generic OAuth-probe failure.</item>
/// </list>
/// Each Core 4 service still keeps its own bespoke api-key middleware (because the gate policies
/// differ — risk-aware path matching for workflow orchestrator, env-var fallback for knowledge-memory service, dev-mode bypass
/// for AiSessionPersistence), but they share these helpers so the conformance behavior matches
/// across the portfolio.
/// </summary>
public static class McpAuthChallengeWriter
{
    /// <summary>The canonical X-Api-Key header name used across Core 4 services.</summary>
    public const string ApiKeyHeader = "X-Api-Key";

    /// <summary>
    /// The A2A 1.0 agent card discovery path. Public per the A2A spec — every host that
    /// runs <c>MapA2AServer()</c> exposes its AgentCard here for unauthenticated discovery.
    /// </summary>
    public const string A2AAgentCardPath = "/.well-known/agent.json";

    /// <summary>
    /// Returns true when the request targets a public discovery endpoint that <em>must not</em>
    /// be gated by api-key auth, per the MCP November 2025 auth spec and the A2A 1.0 spec:
    /// <list type="bullet">
    /// <item><c>/.well-known/oauth-protected-resource</c> (RFC 9728)</item>
    /// <item><c>/.well-known/oauth-authorization-server</c> (RFC 8414)</item>
    /// <item><c>/.well-known/agent.json</c> (A2A 1.0 agent card discovery — story 5fddc4cd)</item>
    /// </list>
    /// Service-side api-key middleware should defer to this helper so all Core 4 services
    /// stay aligned on which discovery paths are public.
    /// </summary>
    public static bool IsPublicMcpDiscoveryPath(PathString path)
    {
        if (!path.HasValue)
        {
            return false;
        }

        var value = path.Value!;
        return string.Equals(value, McpDiscoveryEndpointExtensions.DiscoveryPath, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, McpProtectedResourceEndpointExtensions.DiscoveryPath, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, A2AAgentCardPath, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Write a standardized 401 Unauthorized response for an api-key-protected route. Sets the
    /// <c>WWW-Authenticate</c> header to the RFC-7235-compliant <c>ApiKey realm="&lt;realm&gt;",
    /// header="X-Api-Key"</c> challenge and writes a JSON body with the supplied error message.
    /// </summary>
    /// <param name="context">The HTTP context whose response is being written.</param>
    /// <param name="realm">A short identifier for the protected service (e.g. <c>hive_mind</c>).</param>
    /// <param name="message">Error message included in the JSON body. Defaults to a generic missing/invalid message.</param>
    /// <param name="headerName">The api-key header name. Defaults to <see cref="ApiKeyHeader"/>; override for non-standard guards (e.g. <c>X-Credentials-Admin-Key</c>).</param>
    /// <param name="scheme">The auth scheme advertised in the challenge. Defaults to <c>ApiKey</c>.</param>
    public static Task WriteApiKeyUnauthorizedAsync(
        HttpContext context,
        string realm,
        string? message = null,
        string headerName = ApiKeyHeader,
        string scheme = "ApiKey")
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(realm);

        var safeRealm = SanitizeRealm(realm);
        var safeHeader = SanitizeHeader(headerName);
        var safeScheme = string.IsNullOrWhiteSpace(scheme) ? "ApiKey" : scheme.Trim();

        var payload = BuildApiKeyUnauthorizedPayload(safeRealm, safeHeader, safeScheme, message);

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.WWWAuthenticate = $"{safeScheme} realm=\"{safeRealm}\", header=\"{safeHeader}\"";
        return context.Response.WriteAsJsonAsync(payload);
    }

    /// <summary>
    /// Minimal-API counterpart to <see cref="WriteApiKeyUnauthorizedAsync"/>: stamps the
    /// <c>WWW-Authenticate</c> header on the in-flight response and returns an
    /// <see cref="IResult"/> ready to be returned from an endpoint filter or route handler.
    /// </summary>
    public static IResult ApiKeyUnauthorizedResult(
        HttpContext context,
        string realm,
        string? message = null,
        string headerName = ApiKeyHeader,
        string scheme = "ApiKey")
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(realm);

        var safeRealm = SanitizeRealm(realm);
        var safeHeader = SanitizeHeader(headerName);
        var safeScheme = string.IsNullOrWhiteSpace(scheme) ? "ApiKey" : scheme.Trim();

        var payload = BuildApiKeyUnauthorizedPayload(safeRealm, safeHeader, safeScheme, message);

        context.Response.Headers.WWWAuthenticate = $"{safeScheme} realm=\"{safeRealm}\", header=\"{safeHeader}\"";
        return Results.Json(payload, statusCode: StatusCodes.Status401Unauthorized);
    }

    private static object BuildApiKeyUnauthorizedPayload(
        string safeRealm,
        string safeHeader,
        string safeScheme,
        string? message)
    {
        var error = message ?? $"Missing or invalid {safeHeader}.";
        var guidance =
            $"Provide {safeHeader} for service '{safeRealm}' from credential resolver or the configured runtime environment, then retry. Do not paste or log raw key values.";
        var suggestedNextSteps = new[]
        {
            "get_instructions",
            "resolve_credentials_agent_reference",
            $"retry_with_{safeHeader.ToLowerInvariant().Replace("-", "_", StringComparison.Ordinal)}"
        };
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["service"] = safeRealm,
            ["realm"] = safeRealm,
            ["headerName"] = safeHeader,
            ["authScheme"] = safeScheme
        };

        return new
        {
            success = false,
            error,
            errorCode = "UNAUTHORIZED",
            guidance,
            suggestedNextSteps,
            metadata,
            remediation = new McpRemediation
            {
                RemediationKind = "auth",
                ServiceName = safeRealm,
                ErrorCode = "UNAUTHORIZED",
                FailedCapability = $"{safeScheme} {safeHeader}",
                Guidance = guidance,
                SuggestedNextSteps = suggestedNextSteps,
                Metadata = metadata
            }
        };
    }

    private static string SanitizeRealm(string realm)
        => realm.Replace("\"", string.Empty, StringComparison.Ordinal);

    private static string SanitizeHeader(string headerName)
        => string.IsNullOrWhiteSpace(headerName)
            ? ApiKeyHeader
            : headerName.Replace("\"", string.Empty, StringComparison.Ordinal);
}
