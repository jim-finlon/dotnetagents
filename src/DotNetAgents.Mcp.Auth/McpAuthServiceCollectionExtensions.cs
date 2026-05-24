// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetAgents.Mcp.Auth;

/// <summary>DI registration helpers for MCP November 2025 authentication primitives.</summary>
public static class McpAuthServiceCollectionExtensions
{
    /// <summary>
    /// Register <see cref="McpAuthOptions"/> + <see cref="HttpClientMetadataDocumentResolver"/>
    /// (named HttpClient: <c>McpClientMetadata</c>).
    /// </summary>
    public static IServiceCollection AddMcpAuth(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        if (configuration is not null)
        {
            services.Configure<McpAuthOptions>(configuration.GetSection(McpAuthOptions.SectionName));
        }
        else
        {
            services.AddOptions<McpAuthOptions>();
        }

        services.AddHttpClient<IClientMetadataDocumentResolver, HttpClientMetadataDocumentResolver>("McpClientMetadata");
        return services;
    }
}
