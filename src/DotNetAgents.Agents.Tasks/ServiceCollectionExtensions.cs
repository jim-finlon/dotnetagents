using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetAgents.Agents.Tasks;

/// <summary>
/// Extension methods for registering task queue and store services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the in-memory task queue and store to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInMemoryTaskQueue(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<ITaskQueue, InMemoryTaskQueue>();
        services.TryAddSingleton<ITaskStore, InMemoryTaskStore>();
        return services;
    }

    /// <summary>
    /// Adds custom task queue and store implementations to the service collection.
    /// </summary>
    /// <typeparam name="TQueue">The type of the queue implementation.</typeparam>
    /// <typeparam name="TStore">The type of the store implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTaskQueue<TQueue, TStore>(this IServiceCollection services)
        where TQueue : class, ITaskQueue
        where TStore : class, ITaskStore
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<ITaskQueue, TQueue>();
        services.TryAddSingleton<ITaskStore, TStore>();
        return services;
    }
}
