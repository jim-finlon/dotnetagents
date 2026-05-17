using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.StateMachines;

/// <summary>
/// Common state machine patterns for agent lifecycle management.
/// </summary>
public static class StateMachinePatterns
{
    /// <summary>
    /// Creates an Idle-Working pattern state machine.
    /// Pattern: Idle → Working → Idle
    /// </summary>
    /// <typeparam name="TState">The type of the state context.</typeparam>
    /// <param name="logger">Optional logger instance.</param>
    /// <returns>A configured state machine builder.</returns>
    public static StateMachineBuilder<TState> CreateIdleWorkingPattern<TState>(ILogger<AgentStateMachine<TState>>? logger = null)
        where TState : class
    {
        var builder = new StateMachineBuilder<TState>(logger);

        return builder
            .AddState("Idle")
            .AddState("Working")
            .AddTransition("Idle", "Working")
            .AddTransition("Working", "Idle")
            .SetInitialState("Idle");
    }

    /// <summary>
    /// Creates an Error-Recovery pattern state machine.
    /// Pattern: Any → Error → Recovery → Idle
    /// </summary>
    /// <typeparam name="TState">The type of the state context.</typeparam>
    /// <param name="logger">Optional logger instance.</param>
    /// <returns>A configured state machine builder.</returns>
    public static StateMachineBuilder<TState> CreateErrorRecoveryPattern<TState>(ILogger<AgentStateMachine<TState>>? logger = null)
        where TState : class
    {
        var builder = new StateMachineBuilder<TState>(logger);

        return builder
            .AddState("Idle")
            .AddState("Working")
            .AddState("Error", entryAction: _ => { /* Log error */ })
            .AddState("Recovery")
            .AddTransition("Idle", "Working")
            .AddTransition("Working", "Idle")
            .AddTransition("Idle", "Error")
            .AddTransition("Working", "Error")
            .AddTransition("Error", "Recovery")
            .AddTransition("Recovery", "Idle")
            .SetInitialState("Idle");
    }

    /// <summary>
    /// Creates a Workflow State pattern state machine.
    /// Pattern: Uninitialized → Running → Completed/Failed
    /// </summary>
    /// <typeparam name="TState">The type of the state context.</typeparam>
    /// <param name="logger">Optional logger instance.</param>
    /// <returns>A configured state machine builder.</returns>
    public static StateMachineBuilder<TState> CreateWorkflowStatePattern<TState>(ILogger<AgentStateMachine<TState>>? logger = null)
        where TState : class
    {
        var builder = new StateMachineBuilder<TState>(logger);

        return builder
            .AddState("Uninitialized")
            .AddState("Running")
            .AddState("Completed")
            .AddState("Failed")
            .AddTransition("Uninitialized", "Running")
            .AddTransition("Running", "Completed")
            .AddTransition("Running", "Failed")
            .SetInitialState("Uninitialized");
    }

    /// <summary>
    /// Creates a Worker Pool Agent pattern state machine.
    /// Pattern: Available → Busy → CoolingDown → Available
    /// </summary>
    /// <typeparam name="TState">The type of the state context.</typeparam>
    /// <param name="logger">Optional logger instance.</param>
    /// <param name="cooldownDuration">Duration to stay in CoolingDown state.</param>
    /// <returns>A configured state machine.</returns>
    public static IStateMachine<TState> CreateWorkerPoolPattern<TState>(
        ILogger<AgentStateMachine<TState>>? logger = null,
        TimeSpan? cooldownDuration = null)
        where TState : class
    {
        var builder = new StateMachineBuilder<TState>(logger);

        var stateMachine = builder
            .AddState("Available")
            .AddState("Busy")
            .AddState("CoolingDown")
            .AddTransition("Available", "Busy")
            .AddTransition("Busy", "CoolingDown")
            .AddTransition("CoolingDown", "Available")
            .SetInitialState("Available")
            .Build();

        // Add timeout transition for cooldown
        if (cooldownDuration.HasValue && stateMachine is AgentStateMachine<TState> agentStateMachine)
        {
            agentStateMachine.AddTimeoutTransition("CoolingDown", "Available", cooldownDuration.Value);
        }

        return stateMachine;
    }

    /// <summary>
    /// Creates a Supervisor Agent pattern state machine.
    /// Pattern: Monitoring → Analyzing → Delegating → Waiting → Monitoring
    /// Includes error recovery: Any → Error → Monitoring
    /// </summary>
    /// <typeparam name="TState">The type of the state context.</typeparam>
    /// <param name="logger">Optional logger instance.</param>
    /// <param name="waitingTimeout">Timeout duration for Waiting state before returning to Monitoring (default: 30 seconds).</param>
    /// <returns>A configured state machine.</returns>
    public static IStateMachine<TState> CreateSupervisorPattern<TState>(
        ILogger<AgentStateMachine<TState>>? logger = null,
        TimeSpan? waitingTimeout = null)
        where TState : class
    {
        var builder = new StateMachineBuilder<TState>(logger);

        var timeout = waitingTimeout ?? TimeSpan.FromSeconds(30);

        var stateMachine = builder
            .AddState("Monitoring",
                entryAction: ctx => { /* Log entering monitoring state */ })
            .AddState("Analyzing",
                entryAction: ctx => { /* Log entering analyzing state */ })
            .AddState("Delegating",
                entryAction: ctx => { /* Log entering delegating state */ })
            .AddState("Waiting",
                entryAction: ctx => { /* Log entering waiting state */ })
            .AddState("Error",
                entryAction: ctx => { /* Log error state entry */ })
            .AddTransition("Monitoring", "Analyzing",
                guard: ctx => true) // Always allow when tasks arrive
            .AddTransition("Analyzing", "Delegating",
                guard: ctx => true) // Always allow delegation
            .AddTransition("Analyzing", "Waiting",
                guard: ctx => true) // Allow if no delegation needed
            .AddTransition("Delegating", "Waiting",
                guard: ctx => true) // Always transition after delegation
            .AddTransition("Waiting", "Monitoring",
                guard: ctx => true) // Always allow return to monitoring
            // Error transitions from any state
            .AddTransition("Monitoring", "Error",
                guard: ctx => true) // On exception
            .AddTransition("Analyzing", "Error",
                guard: ctx => true) // On exception
            .AddTransition("Delegating", "Error",
                guard: ctx => true) // On exception
            .AddTransition("Waiting", "Error",
                guard: ctx => true) // On exception
            // Recovery from error
            .AddTransition("Error", "Monitoring",
                guard: ctx => true) // After recovery
            .SetInitialState("Monitoring")
            .Build();

        // Add timeout transition for Waiting → Monitoring
        if (stateMachine is AgentStateMachine<TState> agentStateMachine)
        {
            agentStateMachine.AddTimeoutTransition("Waiting", "Monitoring", timeout);
        }

        return stateMachine;
    }
}
