using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// Maps the RFC 8414 / MCP-Nov-2025 discovery endpoint at
/// <c>/.well-known/oauth-authorization-server</c> so MCP clients can find PKCE/CIMD support
/// without a separate handshake.
/// </summary>
public static class McpDiscoveryEndpointExtensions
{
    public const string DiscoveryPath = "/.well-known/oauth-authorization-server";

    /// <summary>
    /// Maps GET <c>/.well-known/oauth-authorization-server</c>. The metadata is built from
    /// <see cref="McpAuthHostingOptions"/> at request time so operator config changes are
    /// reflected without a restart.
    /// </summary>
    public static IEndpointRouteBuilder MapMcpAuthorizationServerMetadata(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet(DiscoveryPath, (HttpContext http) =>
            {
                var hosting = http.RequestServices.GetRequiredService<IOptions<McpAuthHostingOptions>>().Value;
                var metadata = BuildMetadata(hosting, http);
                return Results.Json(metadata);
            })
            .DisableAntiforgery()
            .WithName("McpAuthorizationServerMetadata")
            .WithTags("MCP", "Discovery", "OAuth");

        return endpoints;
    }

    /// <summary>
    /// Build the metadata document for the given options + request context. Public so tests can
    /// assert the shape without standing up an entire web host.
    /// </summary>
    public static McpAuthorizationServerMetadata BuildMetadata(McpAuthHostingOptions options, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(httpContext);
        var origin = ResolveIssuer(options, httpContext);

        return new McpAuthorizationServerMetadata
        {
            Issuer = origin,
            AuthorizationEndpoint = origin + options.AuthorizationEndpointPath,
            TokenEndpoint = origin + options.TokenEndpointPath,
            McpCrossAppAccessSupported = options.AdvertiseCrossAppAccess,
        };
    }

    private static string ResolveIssuer(McpAuthHostingOptions options, HttpContext httpContext)
    {
        if (!string.IsNullOrWhiteSpace(options.Issuer))
        {
            return options.Issuer.TrimEnd('/');
        }
        var request = httpContext.Request;
        return $"{request.Scheme}://{request.Host.Value}".TrimEnd('/');
    }
}
