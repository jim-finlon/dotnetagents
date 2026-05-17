using DotNetAgents.Ecosystem;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetAgents.Agents.Swarm;

/// <summary>
/// Extension methods for registering swarm intelligence services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds swarm intelligence support to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSwarmIntelligence(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the swarm plugin
        services.AddPlugin(new SwarmPlugin());

        return services;
    }
}
