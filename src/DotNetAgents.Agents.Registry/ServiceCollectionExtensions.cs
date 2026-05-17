using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetAgents.Agents.Registry;

/// <summary>
/// Extension methods for registering agent registry services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the in-memory agent registry to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInMemoryAgentRegistry(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IAgentRegistry, InMemoryAgentRegistry>();
        return services;
    }

    /// <summary>
    /// Adds a custom agent registry implementation to the service collection.
    /// </summary>
    /// <typeparam name="TRegistry">The type of the registry implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAgentRegistry<TRegistry>(this IServiceCollection services)
        where TRegistry : class, IAgentRegistry
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IAgentRegistry, TRegistry>();
        return services;
    }

    /// <summary>
    /// Adds a custom agent registry implementation to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">Factory function to create the registry instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAgentRegistry(
        this IServiceCollection services,
        Func<IServiceProvider, IAgentRegistry> factory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);

        services.TryAddSingleton(factory);
        return services;
    }
}
