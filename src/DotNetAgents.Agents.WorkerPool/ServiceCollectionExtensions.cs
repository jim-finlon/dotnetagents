// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetAgents.Agents.WorkerPool;

/// <summary>
/// Extension methods for registering worker pool services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the worker pool to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWorkerPool(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IWorkerPool, WorkerPool>();
        return services;
    }

    /// <summary>
    /// Adds a custom worker pool implementation to the service collection.
    /// </summary>
    /// <typeparam name="TPool">The type of the worker pool implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWorkerPool<TPool>(this IServiceCollection services)
        where TPool : class, IWorkerPool
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IWorkerPool, TPool>();
        return services;
    }

    /// <summary>
    /// Adds the worker pool with optional state machine support to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="stateProviderFactory">Optional factory function to create a state provider.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWorkerPoolWithStateMachine(
        this IServiceCollection services,
        Func<IServiceProvider, IWorkerStateProvider?>? stateProviderFactory = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (stateProviderFactory != null)
        {
            services.TryAddSingleton<IWorkerStateProvider>(sp => stateProviderFactory(sp)!);
        }

        services.TryAddSingleton<IWorkerPool>(sp =>
        {
            var agentRegistry = sp.GetRequiredService<DotNetAgents.Agents.Registry.IAgentRegistry>();
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<WorkerPool>>();
            var taskQueue = sp.GetService<DotNetAgents.Agents.Tasks.ITaskQueue>();

            // Get state provider if factory is registered
            IWorkerStateProvider? stateProvider = null;
            if (stateProviderFactory != null)
            {
                try
                {
                    stateProvider = stateProviderFactory(sp);
                }
                catch
                {
                    // State provider factory failed, continue without it
                }
            }

            return new WorkerPool(
                agentRegistry,
                loadBalancer: null,
                autoScaler: null,
                taskQueue: taskQueue,
                defaultStrategy: LoadBalancing.LoadBalancingStrategy.PriorityBased,
                logger: logger,
                stateProvider: stateProvider);
        });

        return services;
    }
}
