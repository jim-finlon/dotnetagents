using DotNetAgents.Ecosystem;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// Extension methods for registering behavior tree services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds behavior trees support to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBehaviorTrees(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the behavior trees plugin
        services.AddPlugin(new BehaviorTreesPlugin());

        return services;
    }
}
