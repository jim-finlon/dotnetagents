using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetAgents.Agents.Messaging;

/// <summary>
/// Extension methods for registering agent messaging services.
/// Note: For distributed messaging implementations, see:
/// - DotNetAgents.Agents.Messaging.Kafka for Kafka support
/// - DotNetAgents.Agents.Messaging.RabbitMQ for RabbitMQ support (coming soon)
/// - DotNetAgents.Agents.Messaging.Redis for Redis support (coming soon)
/// - DotNetAgents.Agents.Messaging.SignalR for SignalR support (coming soon)
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the in-memory agent message bus to the service collection.
    /// Suitable for single-instance deployments, development, and testing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInMemoryAgentMessageBus(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IAgentMessageBus, InMemoryAgentMessageBus>();
        return services;
    }

    /// <summary>
    /// Adds a custom agent message bus implementation to the service collection.
    /// </summary>
    /// <typeparam name="TMessageBus">The type of the message bus implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAgentMessageBus<TMessageBus>(this IServiceCollection services)
        where TMessageBus : class, IAgentMessageBus
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IAgentMessageBus, TMessageBus>();
        return services;
    }

    /// <summary>
    /// Adds a custom agent message bus implementation to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">Factory function to create the message bus instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAgentMessageBus(
        this IServiceCollection services,
        Func<IServiceProvider, IAgentMessageBus> factory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);

        services.TryAddSingleton(factory);
        return services;
    }
}
