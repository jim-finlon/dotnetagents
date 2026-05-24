// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Agents.StateMachines;

namespace DotNetAgents.Agents.ControlLoops;

/// <summary>
/// Reusable lifecycle patterns for the four canonical DNA service classes.
/// Story a977f802. Each pattern is a static factory that takes a fresh
/// <see cref="AgentStateMachine{TState}"/> + an initial context and wires up
/// the states + transitions for that service class.
///
/// All four patterns produce state machines whose state names line up with
/// the operator-facing vocabulary established by
/// <see cref="LifecycleStateSnapshot"/> (story 121a8afc): Idle, Running,
/// Waiting, Blocked, Degraded, Recovering, CoolingDown, Completed, Sampling,
/// Training, Evaluating, Promoting, Rejecting. Pilot services adopt the
/// vocabulary by adopting the pattern.
///
/// Patterns are configurable — callers can extend the returned state machine
/// with service-specific states + transitions before <see cref="AgentStateMachine{TState}.SetInitialState"/>
/// is called by their bootstrap code.
/// </summary>
public static class LifecyclePatterns
{
    /// <summary>State name constants. Same vocabulary across all four patterns.</summary>
    public static class States
    {
        public const string Idle = "Idle";
        public const string Running = "Running";
        public const string Waiting = "Waiting";
        public const string Blocked = "Blocked";
        public const string Degraded = "Degraded";
        public const string Recovering = "Recovering";
        public const string CoolingDown = "CoolingDown";
        public const string Completed = "Completed";

        // Evolutionary-specific
        public const string Sampling = "Sampling";
        public const string Training = "Training";
        public const string Evaluating = "Evaluating";
        public const string Promoting = "Promoting";
        public const string Rejecting = "Rejecting";
    }

    /// <summary>
    /// Durable workers (think SDLC autonomous loop, ingestion pipelines):
    ///   Idle → Running → CoolingDown → Idle
    ///        ↘ Degraded ↔ Recovering ↗
    ///        ↘ Blocked → Recovering
    ///        → Completed (terminal)
    /// </summary>
    public static AgentStateMachine<TState> DurableWorker<TState>(AgentStateMachine<TState>? machine = null) where TState : class
    {
        var sm = machine ?? new AgentStateMachine<TState>();
        sm.AddState(States.Idle);
        sm.AddState(States.Running);
        sm.AddState(States.CoolingDown);
        sm.AddState(States.Degraded);
        sm.AddState(States.Recovering);
        sm.AddState(States.Blocked);
        sm.AddState(States.Completed);

        sm.AddTransition(States.Idle, States.Running);
        sm.AddTransition(States.Running, States.CoolingDown);
        sm.AddTransition(States.CoolingDown, States.Idle);
        sm.AddTransition(States.Running, States.Degraded);
        sm.AddTransition(States.Degraded, States.Recovering);
        sm.AddTransition(States.Recovering, States.Idle);
        sm.AddTransition(States.Recovering, States.Degraded);
        sm.AddTransition(States.Running, States.Blocked);
        sm.AddTransition(States.Blocked, States.Recovering);
        sm.AddTransition(States.Running, States.Completed);
        sm.AddTransition(States.Idle, States.Completed);

        sm.SetInitialState(States.Idle);
        return sm;
    }

    /// <summary>
    /// Reactive/background services (event handlers, listeners):
    ///   Idle → Running → Idle  (per-event tick)
    ///        ↘ Degraded ↔ Recovering ↗
    ///        → Completed (terminal: shutdown)
    /// </summary>
    public static AgentStateMachine<TState> ReactiveService<TState>(AgentStateMachine<TState>? machine = null) where TState : class
    {
        var sm = machine ?? new AgentStateMachine<TState>();
        sm.AddState(States.Idle);
        sm.AddState(States.Running);
        sm.AddState(States.Degraded);
        sm.AddState(States.Recovering);
        sm.AddState(States.Completed);

        sm.AddTransition(States.Idle, States.Running);
        sm.AddTransition(States.Running, States.Idle);
        sm.AddTransition(States.Running, States.Degraded);
        sm.AddTransition(States.Degraded, States.Recovering);
        sm.AddTransition(States.Recovering, States.Idle);
        sm.AddTransition(States.Idle, States.Completed);
        sm.AddTransition(States.Degraded, States.Completed);

        sm.SetInitialState(States.Idle);
        return sm;
    }

    /// <summary>
    /// Guarded control-plane services (operator approvals, deploy gates):
    ///   Idle → Waiting → Running → Completed
    ///        ↘ Blocked → Waiting (retry)
    ///        ↘ Degraded ↔ Recovering ↗
    /// Waiting is the canonical "needs operator input" state.
    /// </summary>
    public static AgentStateMachine<TState> ControlPlane<TState>(AgentStateMachine<TState>? machine = null) where TState : class
    {
        var sm = machine ?? new AgentStateMachine<TState>();
        sm.AddState(States.Idle);
        sm.AddState(States.Waiting);
        sm.AddState(States.Running);
        sm.AddState(States.Blocked);
        sm.AddState(States.Degraded);
        sm.AddState(States.Recovering);
        sm.AddState(States.Completed);

        sm.AddTransition(States.Idle, States.Waiting);
        sm.AddTransition(States.Waiting, States.Running);
        sm.AddTransition(States.Waiting, States.Blocked);
        sm.AddTransition(States.Running, States.Completed);
        sm.AddTransition(States.Running, States.Degraded);
        sm.AddTransition(States.Blocked, States.Waiting);
        sm.AddTransition(States.Blocked, States.Idle);
        sm.AddTransition(States.Degraded, States.Recovering);
        sm.AddTransition(States.Recovering, States.Idle);
        sm.AddTransition(States.Recovering, States.Running);

        sm.SetInitialState(States.Idle);
        return sm;
    }

    /// <summary>
    /// Evolutionary/experiment services (Evaluation Sandbox trials, JARVIS genome):
    ///   Sampling → Training → Evaluating → (Promoting | Rejecting) → CoolingDown → Sampling
    ///                                                              ↘ Completed (terminal)
    ///   With Degraded ↔ Recovering branches off Training/Evaluating.
    /// </summary>
    public static AgentStateMachine<TState> EvolutionaryService<TState>(AgentStateMachine<TState>? machine = null) where TState : class
    {
        var sm = machine ?? new AgentStateMachine<TState>();
        sm.AddState(States.Sampling);
        sm.AddState(States.Training);
        sm.AddState(States.Evaluating);
        sm.AddState(States.Promoting);
        sm.AddState(States.Rejecting);
        sm.AddState(States.CoolingDown);
        sm.AddState(States.Degraded);
        sm.AddState(States.Recovering);
        sm.AddState(States.Completed);

        sm.AddTransition(States.Sampling, States.Training);
        sm.AddTransition(States.Training, States.Evaluating);
        sm.AddTransition(States.Evaluating, States.Promoting);
        sm.AddTransition(States.Evaluating, States.Rejecting);
        sm.AddTransition(States.Promoting, States.CoolingDown);
        sm.AddTransition(States.Rejecting, States.CoolingDown);
        sm.AddTransition(States.CoolingDown, States.Sampling);
        sm.AddTransition(States.CoolingDown, States.Completed);
        sm.AddTransition(States.Training, States.Degraded);
        sm.AddTransition(States.Evaluating, States.Degraded);
        sm.AddTransition(States.Degraded, States.Recovering);
        sm.AddTransition(States.Recovering, States.Sampling);

        sm.SetInitialState(States.Sampling);
        return sm;
    }

    /// <summary>
    /// Helper for snapshotting a state machine into the
    /// <see cref="LifecycleStateSnapshot"/> contract from story 121a8afc.
    /// Phase defaults to the state name; callers that track richer phase
    /// info (e.g. "ImplementingStory" inside Running) can pass it explicitly.
    /// </summary>
    public static LifecycleStateSnapshot Snapshot<TState>(
        AgentStateMachine<TState> sm,
        DateTime enteredAtUtc,
        string? phase = null,
        string? reason = null,
        bool isTerminal = false) where TState : class =>
        new(sm.CurrentState, phase ?? sm.CurrentState, enteredAtUtc, reason, isTerminal);
}
