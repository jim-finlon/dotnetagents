// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Mcp.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// One-line setup for MCP November 2025 authentication on a hosting service. Composes
/// <see cref="DotNetAgents.Mcp.Auth.McpAuthOptions"/> + the hosting-side
/// <see cref="McpAuthHostingOptions"/> + the <see cref="IPkceChallengeStore"/>.
/// </summary>
public static class McpAuthServerExtensions
{
    /// <summary>
    /// Register the November 2025 MCP auth surface on the host. Caller maps endpoints via
    /// <see cref="MapMcpAuth"/> after building the app.
    /// </summary>
    public static IServiceCollection AddMcpAuthServer(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMcpAuth(configuration);

        if (configuration is not null)
        {
            services.Configure<McpAuthHostingOptions>(configuration.GetSection(McpAuthHostingOptions.SectionName));
        }
        else
        {
            services.AddOptions<McpAuthHostingOptions>();
        }

        services.TryAddSingleton<IPkceChallengeStore, InMemoryPkceChallengeStore>();
        AddPkceConsentStore(services, configuration);
        services.TryAddSingleton<IMcpPkceTokenIssuer, DefaultMcpPkceTokenIssuer>();
        services.TryAddSingleton<McpAuthEnabledMarker>();
        return services;
    }

    /// <summary>
    /// Map the discovery + token-exchange endpoints + the consent UI per
    /// story 1095b26a. Token endpoint is conditional on <see cref="McpAuthMode"/>:
    /// <see cref="McpAuthMode.Legacy"/> hides it.
    /// </summary>
    /// <param name="endpoints">Routing builder.</param>
    /// <param name="serviceName">
    /// Display name shown on the consent page (e.g. "credential resolver").
    /// Defaults to "DNA MCP" so existing callers compile without changes.
    /// </param>
    public static IEndpointRouteBuilder MapMcpAuth(
        this IEndpointRouteBuilder endpoints,
        string serviceName = "DNA MCP")
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapMcpAuthorizationServerMetadata();
        endpoints.MapMcpProtectedResourceMetadata();
        endpoints.MapMcpPkceTokenEndpoint();

        // Story 1095b26a — replace the legacy authorization_not_supported short-circuit
        // with the real consent UI. Hosts that want the legacy behavior can opt out by
        // skipping this call and mapping their own GET /.mcp/oauth/authorize handler.
        endpoints.MapMcpPkceConsentEndpoints(serviceName);

        return endpoints;
    }

    private static void AddPkceConsentStore(IServiceCollection services, IConfiguration? configuration)
    {
        var provider = configuration?[McpAuthHostingOptions.SectionName + ":ConsentStoreProvider"];
        if (string.Equals(provider, "File", StringComparison.OrdinalIgnoreCase))
        {
            services.Configure<DurableFilePkceConsentStoreOptions>(options =>
            {
                options.FilePath = configuration?[McpAuthHostingOptions.SectionName + ":ConsentStoreFilePath"];
            });
            services.TryAddSingleton<IPkceConsentStore, DurableFilePkceConsentStore>();
            return;
        }

        services.TryAddSingleton<IPkceConsentStore, InMemoryPkceConsentStore>();
    }
}
