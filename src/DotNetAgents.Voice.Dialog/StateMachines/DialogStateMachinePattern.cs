// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Agents.StateMachines;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Voice.Dialog.StateMachines;

/// <summary>
/// State machine patterns for dialog lifecycle management.
/// </summary>
public static class DialogStateMachinePattern
{
    /// <summary>
    /// Creates a Dialog state machine pattern for multi-turn conversations.
    /// Pattern: Initial → CollectingInfo → Confirming → Executing → Completed
    /// Includes error recovery: Any → Error → Initial
    /// </summary>
    /// <typeparam name="TState">The type of the state context.</typeparam>
    /// <param name="logger">Optional logger instance.</param>
    /// <param name="collectingTimeout">Timeout duration for CollectingInfo state before transitioning to Error (default: 5 minutes).</param>
    /// <param name="confirmingTimeout">Timeout duration for Confirming state before transitioning to Error (default: 2 minutes).</param>
    /// <returns>A configured state machine.</returns>
    public static IStateMachine<TState> CreateDialogPattern<TState>(
        ILogger<AgentStateMachine<TState>>? logger = null,
        TimeSpan? collectingTimeout = null,
        TimeSpan? confirmingTimeout = null)
        where TState : class
    {
        var builder = new StateMachineBuilder<TState>(logger);

        var collectingTimeoutDuration = collectingTimeout ?? TimeSpan.FromMinutes(5);
        var confirmingTimeoutDuration = confirmingTimeout ?? TimeSpan.FromMinutes(2);

        var stateMachine = builder
            .AddState("Initial",
                entryAction: ctx => { /* Log entering initial state */ })
            .AddState("CollectingInfo",
                entryAction: ctx => { /* Log entering collecting info state */ })
            .AddState("Confirming",
                entryAction: ctx => { /* Log entering confirming state */ })
            .AddState("Executing",
                entryAction: ctx => { /* Log entering executing state */ })
            .AddState("Completed",
                entryAction: ctx => { /* Log entering completed state */ })
            .AddState("Error",
                entryAction: ctx => { /* Log error state entry */ })
            .AddTransition("Initial", "CollectingInfo",
                guard: ctx => true) // On dialog start
            .AddTransition("CollectingInfo", "Confirming",
                guard: ctx => true) // On all info collected
            .AddTransition("CollectingInfo", "Executing",
                guard: ctx => true) // Skip confirmation if not needed
            .AddTransition("Confirming", "Executing",
                guard: ctx => true) // On confirmation received
            .AddTransition("Executing", "Completed",
                guard: ctx => true) // On execution complete
            // Error transitions from any state
            .AddTransition("Initial", "Error",
                guard: ctx => true) // On exception
            .AddTransition("CollectingInfo", "Error",
                guard: ctx => true) // On exception
            .AddTransition("Confirming", "Error",
                guard: ctx => true) // On exception
            .AddTransition("Executing", "Error",
                guard: ctx => true) // On exception
            // Recovery from error
            .AddTransition("Error", "Initial",
                guard: ctx => true) // After recovery, restart dialog
            .SetInitialState("Initial")
            .Build();

        // Add timeout transitions
        if (stateMachine is AgentStateMachine<TState> agentStateMachine)
        {
            // CollectingInfo timeout: CollectingInfo → Error
            agentStateMachine.AddTimeoutTransition("CollectingInfo", "Error", collectingTimeoutDuration);

            // Confirming timeout: Confirming → Error
            agentStateMachine.AddTimeoutTransition("Confirming", "Error", confirmingTimeoutDuration);
        }

        return stateMachine;
    }
}
