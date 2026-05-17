using DotNetAgents.Ecosystem;
using DotNetAgents.Mcp.Adapters;
using DotNetAgents.Mcp.Abstractions;
using DotNetAgents.Mcp.Configuration;
using DotNetAgents.Mcp.Models;
using DotNetAgents.Mcp.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Mcp;

/// <summary>
/// Extension methods for registering MCP services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the shared learning-event projector used by orchestrators and agents.
    /// </summary>
    public static IServiceCollection AddAgentLearningProjection(
        this IServiceCollection services,
        Action<AgentLearningProjectionOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new AgentLearningProjectionOptions();
        configure?.Invoke(options);

        services.AddHttpClient();
        services.TryAddSingleton(options);
        services.TryAddSingleton<IAgentLearningProjector, AgentLearningProjector>();
        return services;
    }

    /// <summary>
    /// Adds MCP client services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMcpClients(
        this IServiceCollection services,
        Action<McpClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the MCP plugin
        services.AddPlugin(new McpPlugin());

        var options = new McpClientOptions();
        configure?.Invoke(options);

        // Add HTTP client factory if not already added
        services.AddHttpClient();

        // Register MCP client factory
        services.TryAddSingleton<IMcpClientFactory>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var logger = sp.GetRequiredService<ILogger<McpClient>>();
            var configs = options.ServiceConfigs ?? new List<McpServiceConfig>();
            return new McpClientFactory(httpClientFactory, configs, logger);
        });

        // Register tool registry
        services.TryAddSingleton<IMcpToolRegistry, McpToolRegistry>();

        // Register adapter router
        services.TryAddScoped<IMcpAdapterRouter, McpAdapterRouter>();

        // Register MCP-tool-to-ITool adapter factory (story RW-5 e05c7b1e). Generic primitive
        // — per-role allowlists and forbidden-tools deny-lists live in the consuming layer.
        services.TryAddSingleton<DotNetAgents.Mcp.Adapters.IMcpAgentToolFactory, DotNetAgents.Mcp.Adapters.McpAgentToolFactory>();

        return services;
    }

    /// <summary>
    /// Adds an MCP service configuration.
    /// </summary>
    /// <param name="options">The MCP client options.</param>
    /// <param name="config">The service configuration.</param>
    /// <returns>The options for chaining.</returns>
    public static McpClientOptions AddService(this McpClientOptions options, McpServiceConfig config)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(config);

        options.ServiceConfigs ??= new List<McpServiceConfig>();
        options.ServiceConfigs.Add(config);
        return options;
    }

    /// <summary>
    /// Adds an MCP service configuration using a builder action.
    /// </summary>
    /// <param name="options">The MCP client options.</param>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="baseUrl">The base URL of the service.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The options for chaining.</returns>
    public static McpClientOptions AddService(
        this McpClientOptions options,
        string serviceName,
        string baseUrl,
        Action<McpServiceConfig>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

        var config = new McpServiceConfig
        {
            ServiceName = serviceName,
            BaseUrl = baseUrl
        };

        configure?.Invoke(config);
        return options.AddService(config);
    }
}

/// <summary>
/// Options for MCP client configuration.
/// </summary>
public class McpClientOptions
{
    /// <summary>
    /// Gets or sets the list of service configurations.
    /// </summary>
    public List<McpServiceConfig>? ServiceConfigs { get; set; }
}
