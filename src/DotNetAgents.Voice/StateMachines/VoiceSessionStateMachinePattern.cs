using DotNetAgents.Agents.StateMachines;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Voice.StateMachines;

/// <summary>
/// State machine patterns for voice session lifecycle management.
/// </summary>
public static class VoiceSessionStateMachinePattern
{
    /// <summary>
    /// Creates a Voice Session state machine pattern.
    /// Pattern: Idle → Listening → Processing → Responding → Idle
    /// Includes error recovery: Any → Error → Idle
    /// </summary>
    /// <typeparam name="TState">The type of the state context.</typeparam>
    /// <param name="logger">Optional logger instance.</param>
    /// <param name="listeningTimeout">Timeout duration for Listening state before returning to Idle (default: 30 seconds).</param>
    /// <param name="processingTimeout">Timeout duration for Processing state before transitioning to Error (default: 60 seconds).</param>
    /// <returns>A configured state machine.</returns>
    public static IStateMachine<TState> CreateVoiceSessionPattern<TState>(
        ILogger<AgentStateMachine<TState>>? logger = null,
        TimeSpan? listeningTimeout = null,
        TimeSpan? processingTimeout = null)
        where TState : class
    {
        var builder = new StateMachineBuilder<TState>(logger);

        var listeningTimeoutDuration = listeningTimeout ?? TimeSpan.FromSeconds(30);
        var processingTimeoutDuration = processingTimeout ?? TimeSpan.FromSeconds(60);

        var stateMachine = builder
            .AddState("Idle",
                entryAction: ctx => { /* Log entering idle state */ })
            .AddState("Listening",
                entryAction: ctx => { /* Log entering listening state */ })
            .AddState("Processing",
                entryAction: ctx => { /* Log entering processing state */ })
            .AddState("Responding",
                entryAction: ctx => { /* Log entering responding state */ })
            .AddState("Error",
                entryAction: ctx => { /* Log error state entry */ })
            .AddTransition("Idle", "Listening",
                guard: ctx => true) // On voice input detected
            .AddTransition("Listening", "Processing",
                guard: ctx => true) // On input complete
            .AddTransition("Processing", "Responding",
                guard: ctx => true) // On response ready
            .AddTransition("Responding", "Idle",
                guard: ctx => true) // On response complete
            // Error transitions from any state
            .AddTransition("Idle", "Error",
                guard: ctx => true) // On exception
            .AddTransition("Listening", "Error",
                guard: ctx => true) // On exception
            .AddTransition("Processing", "Error",
                guard: ctx => true) // On exception
            .AddTransition("Responding", "Error",
                guard: ctx => true) // On exception
            // Recovery from error
            .AddTransition("Error", "Idle",
                guard: ctx => true) // After recovery
            .SetInitialState("Idle")
            .Build();

        // Add timeout transitions
        if (stateMachine is AgentStateMachine<TState> agentStateMachine)
        {
            // Listening timeout: Listening → Idle
            agentStateMachine.AddTimeoutTransition("Listening", "Idle", listeningTimeoutDuration);

            // Processing timeout: Processing → Error
            agentStateMachine.AddTimeoutTransition("Processing", "Error", processingTimeoutDuration);
        }

        return stateMachine;
    }
}
