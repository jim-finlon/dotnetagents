using DotNetAgents.Tasks.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetAgents.Tasks;

/// <summary>
/// Extension methods for registering DotNetAgents.Tasks services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds DotNetAgents.Tasks services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureStore">Optional action to configure the task store. If not provided, uses InMemoryTaskStore.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDotNetAgentsTasks(
        this IServiceCollection services,
        Action<IServiceCollection>? configureStore = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // Register store
        if (configureStore != null)
        {
            configureStore(services);
        }
        else
        {
            services.AddSingleton<ITaskStore, InMemoryTaskStore>();
        }

        // Register manager
        services.AddScoped<ITaskManager, TaskManager>();

        return services;
    }
}
