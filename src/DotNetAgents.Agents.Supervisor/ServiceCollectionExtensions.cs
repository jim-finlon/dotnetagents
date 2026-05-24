// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Agents.Messaging;
using DotNetAgents.Agents.Registry;
using DotNetAgents.Agents.Tasks;
using DotNetAgents.Agents.WorkerPool;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetAgents.Agents.Supervisor;

/// <summary>
/// Extension methods for registering supervisor agent services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the supervisor agent to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSupervisorAgent(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<ISupervisorAgent, SupervisorAgent>();
        return services;
    }

    /// <summary>
    /// Adds a custom supervisor agent implementation to the service collection.
    /// </summary>
    /// <typeparam name="TSupervisor">The type of the supervisor implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSupervisorAgent<TSupervisor>(this IServiceCollection services)
        where TSupervisor : class, ISupervisorAgent
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<ISupervisorAgent, TSupervisor>();
        return services;
    }

    /// <summary>
    /// Adds the supervisor agent with an optional state machine and task router to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="stateMachineFactory">Optional factory function to create a state machine. If null, supervisor runs without state machine.</param>
    /// <param name="taskRouterFactory">Optional factory function to create a task router. If null, supervisor uses standard routing.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSupervisorAgentWithStateMachine(
        this IServiceCollection services,
        Func<IServiceProvider, ISupervisorStateMachine<SupervisorContext>?>? stateMachineFactory = null,
        Func<IServiceProvider, ITaskRouter?>? taskRouterFactory = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (stateMachineFactory != null)
        {
            services.TryAddSingleton<ISupervisorStateMachine<SupervisorContext>>(sp => stateMachineFactory(sp)!);
        }

        if (taskRouterFactory != null)
        {
            services.TryAddSingleton<ITaskRouter>(sp => taskRouterFactory(sp)!);
        }

        services.TryAddSingleton<ISupervisorAgent>(sp =>
        {
            var agentRegistry = sp.GetRequiredService<DotNetAgents.Agents.Registry.IAgentRegistry>();
            var messageBus = sp.GetRequiredService<DotNetAgents.Agents.Messaging.IAgentMessageBus>();
            var taskQueue = sp.GetRequiredService<DotNetAgents.Agents.Tasks.ITaskQueue>();
            var taskStore = sp.GetRequiredService<DotNetAgents.Agents.Tasks.ITaskStore>();
            var workerPool = sp.GetRequiredService<DotNetAgents.Agents.WorkerPool.IWorkerPool>();
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<SupervisorAgent>>();

            // Get state machine if factory is registered
            ISupervisorStateMachine<SupervisorContext>? stateMachine = null;
            if (stateMachineFactory != null)
            {
                try
                {
                    stateMachine = stateMachineFactory(sp);
                }
                catch
                {
                    // State machine factory failed, continue without it
                }
            }

            // Get task router if factory is registered
            ITaskRouter? taskRouter = null;
            if (taskRouterFactory != null)
            {
                try
                {
                    taskRouter = taskRouterFactory(sp);
                }
                catch
                {
                    // Task router factory failed, continue without it
                }
            }

            return new SupervisorAgent(agentRegistry, messageBus, taskQueue, taskStore, workerPool, logger, stateMachine, taskRouter);
        });

        return services;
    }

    /// <summary>
    /// Adds the reference supervisor/worker-pool pattern used by samples and single-process pilots.
    /// Registers in-memory registry, message bus, task queue/store, worker pool, and supervisor services
    /// so callers can focus on worker registration and routing behavior.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="stateMachineFactory">Optional supervisor state machine factory.</param>
    /// <param name="taskRouterFactory">Optional task router factory.</param>
    /// <param name="stateProviderFactory">Optional worker state provider factory.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSupervisorWorkerReferencePattern(
        this IServiceCollection services,
        Func<IServiceProvider, ISupervisorStateMachine<SupervisorContext>?>? stateMachineFactory = null,
        Func<IServiceProvider, ITaskRouter?>? taskRouterFactory = null,
        Func<IServiceProvider, IWorkerStateProvider?>? stateProviderFactory = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddInMemoryAgentRegistry();
        services.AddInMemoryAgentMessageBus();
        services.AddInMemoryTaskQueue();
        services.AddWorkerPoolWithStateMachine(stateProviderFactory);
        services.AddSupervisorAgentWithStateMachine(stateMachineFactory, taskRouterFactory);

        return services;
    }
}
