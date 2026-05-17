using DotNetAgents.Ecosystem;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetAgents.Agents.StateMachines;

/// <summary>
/// Extension methods for registering state machine services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds state machines support to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddStateMachines(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the state machines plugin
        services.AddPlugin(new StateMachinesPlugin());

        return services;
    }
}
