using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// Maps the RFC 9728 / MCP-Nov-2025 protected-resource discovery endpoint at
/// <c>/.well-known/oauth-protected-resource</c>. Required so MCP clients that probe after a 401
/// can discover the authorization server without falling back to ad-hoc OAuth probing (which
/// surfaces as a misleading "OAuth error" in clients like Cursor and Claude Code when the real
/// cause was a missing/wrong API key).
/// </summary>
public static class McpProtectedResourceEndpointExtensions
{
    public const string DiscoveryPath = "/.well-known/oauth-protected-resource";

    /// <summary>
    /// Maps GET <c>/.well-known/oauth-protected-resource</c>. The metadata advertises this resource's
    /// own host as the authorization server (DNA Core 4 services double as their own AS for the
    /// PKCE token endpoint).
    /// </summary>
    public static IEndpointRouteBuilder MapMcpProtectedResourceMetadata(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet(DiscoveryPath, (HttpContext http) =>
            {
                var hosting = http.RequestServices.GetRequiredService<IOptions<McpAuthHostingOptions>>().Value;
                var metadata = BuildMetadata(hosting, http);
                return Results.Json(metadata);
            })
            .DisableAntiforgery()
            .WithName("McpProtectedResourceMetadata")
            .WithTags("MCP", "Discovery", "OAuth");

        return endpoints;
    }

    /// <summary>
    /// Build the metadata document for the given options + request context. Public so tests can
    /// assert the shape without standing up an entire web host.
    /// </summary>
    public static McpProtectedResourceMetadata BuildMetadata(McpAuthHostingOptions options, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(httpContext);

        var origin = ResolveOrigin(options, httpContext);

        return new McpProtectedResourceMetadata
        {
            Resource = origin,
            AuthorizationServers = new List<string> { origin },
        };
    }

    private static string ResolveOrigin(McpAuthHostingOptions options, HttpContext httpContext)
    {
        if (!string.IsNullOrWhiteSpace(options.Issuer))
        {
            return options.Issuer.TrimEnd('/');
        }
        var request = httpContext.Request;
        return $"{request.Scheme}://{request.Host.Value}".TrimEnd('/');
    }
}
