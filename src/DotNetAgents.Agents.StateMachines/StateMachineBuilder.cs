using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.StateMachines;

/// <summary>
/// Fluent builder API for creating state machines.
/// </summary>
/// <typeparam name="TState">The type of the state context.</typeparam>
public class StateMachineBuilder<TState> where TState : class
{
    private readonly AgentStateMachine<TState> _stateMachine;
    private string? _initialState;

    /// <summary>
    /// Initializes a new instance of the <see cref="StateMachineBuilder{TState}"/> class.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    public StateMachineBuilder(ILogger<AgentStateMachine<TState>>? logger = null)
    {
        _stateMachine = new AgentStateMachine<TState>(logger);
    }

    /// <summary>
    /// Adds a state to the state machine.
    /// </summary>
    /// <param name="name">The name of the state.</param>
    /// <param name="entryAction">Optional action to execute when entering the state.</param>
    /// <param name="exitAction">Optional action to execute when exiting the state.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public StateMachineBuilder<TState> AddState(
        string name,
        Action<TState>? entryAction = null,
        Action<TState>? exitAction = null)
    {
        _stateMachine.AddState(name, entryAction, exitAction);
        return this;
    }

    /// <summary>
    /// Adds a state to the state machine with asynchronous actions.
    /// </summary>
    /// <param name="name">The name of the state.</param>
    /// <param name="entryActionAsync">Optional asynchronous action to execute when entering the state.</param>
    /// <param name="exitActionAsync">Optional asynchronous action to execute when exiting the state.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public StateMachineBuilder<TState> AddStateAsync(
        string name,
        Func<TState, CancellationToken, Task>? entryActionAsync = null,
        Func<TState, CancellationToken, Task>? exitActionAsync = null)
    {
        _stateMachine.AddState(name, null, null, entryActionAsync, exitActionAsync);
        return this;
    }

    /// <summary>
    /// Adds a transition between states.
    /// </summary>
    /// <param name="fromState">The source state.</param>
    /// <param name="toState">The target state.</param>
    /// <param name="guard">Optional guard condition that must be true for the transition to be allowed.</param>
    /// <param name="onTransition">Optional action to execute during the transition.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public StateMachineBuilder<TState> AddTransition(
        string fromState,
        string toState,
        Func<TState, bool>? guard = null,
        Action<TState>? onTransition = null)
    {
        _stateMachine.AddTransition(fromState, toState, guard, onTransition);
        return this;
    }

    /// <summary>
    /// Adds a transition between states with asynchronous action.
    /// </summary>
    /// <param name="fromState">The source state.</param>
    /// <param name="toState">The target state.</param>
    /// <param name="guard">Optional guard condition that must be true for the transition to be allowed.</param>
    /// <param name="onTransitionAsync">Optional asynchronous action to execute during the transition.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public StateMachineBuilder<TState> AddTransitionAsync(
        string fromState,
        string toState,
        Func<TState, bool>? guard = null,
        Func<TState, CancellationToken, Task>? onTransitionAsync = null)
    {
        _stateMachine.AddTransition(fromState, toState, guard, null, onTransitionAsync);
        return this;
    }

    /// <summary>
    /// Sets the initial state of the state machine.
    /// </summary>
    /// <param name="stateName">The name of the initial state.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public StateMachineBuilder<TState> SetInitialState(string stateName)
    {
        _initialState = stateName;
        _stateMachine.SetInitialState(stateName);
        return this;
    }

    /// <summary>
    /// Builds and validates the state machine.
    /// </summary>
    /// <returns>The configured state machine instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the state machine is invalid (e.g., no initial state, orphaned states, invalid transitions).</exception>
    public IStateMachine<TState> Build()
    {
        Validate();
        return _stateMachine;
    }

    /// <summary>
    /// Validates the state machine configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when validation fails.</exception>
    private void Validate()
    {
        if (string.IsNullOrEmpty(_initialState))
        {
            throw new InvalidOperationException("State machine must have an initial state. Call SetInitialState() before building.");
        }

        // Additional validation can be added here:
        // - Check for orphaned states (states with no incoming or outgoing transitions)
        // - Check for unreachable states
        // - Check for cycles (if needed)
    }
}
