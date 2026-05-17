using DotNetAgents.Workflow.Session.Bootstrap;
using DotNetAgents.Workflow.Session.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetAgents.Workflow.Session;

/// <summary>
/// Extension methods for registering DotNetAgents.Workflow.Session services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds DotNetAgents.Workflow.Session services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureStores">Optional action to configure the stores. If not provided, uses in-memory stores.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDotNetAgentsWorkflowSession(
        this IServiceCollection services,
        Action<IServiceCollection>? configureStores = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // Register stores
        if (configureStores != null)
        {
            configureStores(services);
        }
        else
        {
            services.AddSingleton<ISnapshotStore, InMemorySnapshotStore>();
            services.AddSingleton<IMilestoneStore, InMemoryMilestoneStore>();
            services.AddSingleton<ISessionContextStore, InMemorySessionContextStore>();
        }

        // Register bootstrap generator
        services.AddSingleton<IBootstrapGenerator, BootstrapGenerator>();

        // Register manager
        services.AddScoped<ISessionManager, SessionManager>();

        return services;
    }
}
