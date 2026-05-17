using DotNetAgents.Abstractions.Models;
using DotNetAgents.Voice.IntentClassification;
using DotNetAgents.Voice.Notifications;
using DotNetAgents.Voice.Parsing;
using DotNetAgents.Voice.StateMachines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Voice;

/// <summary>
/// Extension methods for registering voice command services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds voice command processing services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVoiceCommands(
        this IServiceCollection services,
        Action<VoiceCommandOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new VoiceCommandOptions();
        configure?.Invoke(options);

        // Register taxonomy registry (singleton - shared across all classifiers)
        services.TryAddSingleton<IIntentTaxonomyRegistry, IntentTaxonomyRegistry>();

        // Register command template registry (singleton)
        services.TryAddSingleton<Commands.ICommandTemplateRegistry, Commands.CommandTemplateRegistry>();

        // Register intent classifier
        services.TryAddScoped<IIntentClassifier>(sp =>
        {
            var llmModel = sp.GetRequiredService<ILLMModel<ChatMessage[], ChatMessage>>();
            var logger = sp.GetRequiredService<ILogger<LLMIntentClassifier>>();
            var taxonomyRegistry = sp.GetRequiredService<IIntentTaxonomyRegistry>();
            return new LLMIntentClassifier(llmModel, logger, taxonomyRegistry);
        });

        // Register command parser
        services.TryAddScoped<ICommandParser, CommandParser>();

        return services;
    }

    /// <summary>
    /// Adds command workflow orchestration services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCommandOrchestration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<Orchestration.ICommandWorkflowOrchestrator, Orchestration.CommandWorkflowOrchestrator>();

        return services;
    }

    /// <summary>
    /// Adds command workflow orchestration services with optional voice session state machine and behavior tree support.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="stateMachineFactory">Optional factory function to create a voice session state machine. If null, orchestrator runs without state machine.</param>
    /// <param name="behaviorTreeFactory">Optional factory function to create a command processing behavior tree. If null, orchestrator runs without behavior tree.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCommandOrchestrationWithStateMachine(
        this IServiceCollection services,
        Func<IServiceProvider, StateMachines.IVoiceSessionStateMachine<StateMachines.VoiceSessionContext>?>? stateMachineFactory = null,
        Func<IServiceProvider, BehaviorTrees.CommandProcessingBehaviorTree?>? behaviorTreeFactory = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Store factories in local variables for closure
        var stateMachineFactoryLocal = stateMachineFactory;
        var behaviorTreeFactoryLocal = behaviorTreeFactory;

        services.TryAddScoped<Orchestration.ICommandWorkflowOrchestrator>(sp =>
        {
            var parser = sp.GetRequiredService<Parsing.ICommandParser>();
            var adapterRouter = sp.GetRequiredService<Mcp.Routing.IMcpAdapterRouter>();
            var logger = sp.GetRequiredService<ILogger<Orchestration.CommandWorkflowOrchestrator>>();
            var notificationService = sp.GetService<ICommandNotificationService>();

            // Get state machine if factory is registered
            StateMachines.IVoiceSessionStateMachine<StateMachines.VoiceSessionContext>? sessionStateMachine = null;
            if (stateMachineFactoryLocal != null)
            {
                try
                {
                    sessionStateMachine = stateMachineFactoryLocal(sp);
                }
                catch
                {
                    // State machine factory failed, continue without it
                }
            }

            // Get behavior tree if factory is registered
            BehaviorTrees.CommandProcessingBehaviorTree? behaviorTree = null;
            if (behaviorTreeFactoryLocal != null)
            {
                try
                {
                    behaviorTree = behaviorTreeFactoryLocal(sp);
                }
                catch
                {
                    // Behavior tree factory failed, continue without it
                }
            }

            return new Orchestration.CommandWorkflowOrchestrator(
                parser,
                adapterRouter,
                logger,
                notificationService,
                sessionStateMachine,
                behaviorTree);
        });

        return services;
    }

    /// <summary>
    /// Adds a custom intent classifier implementation.
    /// </summary>
    /// <typeparam name="T">The intent classifier implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddIntentClassifier<T>(this IServiceCollection services)
        where T : class, IIntentClassifier
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddScoped<IIntentClassifier, T>();
        return services;
    }
}

/// <summary>
/// Options for voice command processing.
/// </summary>
public class VoiceCommandOptions
{
    /// <summary>
    /// Gets or sets whether to use structured output mode for intent classification.
    /// </summary>
    public bool UseStructuredOutput { get; set; } = true;

    /// <summary>
    /// Gets or sets the default confidence threshold for intent classification.
    /// </summary>
    public double ConfidenceThreshold { get; set; } = 0.7;
}
