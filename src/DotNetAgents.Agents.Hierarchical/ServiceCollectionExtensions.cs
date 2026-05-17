using DotNetAgents.Ecosystem;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetAgents.Agents.Hierarchical;

/// <summary>
/// Extension methods for registering hierarchical agent services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds hierarchical agent organization support to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHierarchicalAgents(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the hierarchical plugin
        services.AddPlugin(new HierarchicalPlugin());

        return services;
    }
}
