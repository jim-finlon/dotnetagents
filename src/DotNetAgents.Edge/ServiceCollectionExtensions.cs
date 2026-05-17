using DotNetAgents.Ecosystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetAgents.Edge;

/// <summary>
/// Extension methods for registering edge computing services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds edge computing support to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDotNetAgentsEdge(
        this IServiceCollection services,
        Action<EdgeModelConfiguration>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the edge plugin
        services.AddPlugin(new EdgePlugin());

        // Register offline cache
        services.TryAddSingleton<IOfflineCache, InMemoryOfflineCache>();

        // Register edge model configuration
        if (configure != null)
        {
            var config = new EdgeModelConfiguration();
            configure(config);
            services.TryAddSingleton(config);
        }

        return services;
    }

    /// <summary>
    /// Adds an edge agent to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEdgeAgent(
        this IServiceCollection services,
        Action<EdgeModelConfiguration>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDotNetAgentsEdge(configure);
        services.TryAddScoped<IEdgeAgent, EdgeAgent>();

        return services;
    }
}
