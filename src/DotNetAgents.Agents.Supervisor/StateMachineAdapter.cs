// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Agents.Supervisor;

/// <summary>
/// Adapter to wrap an external state machine implementation for use with SupervisorAgent.
/// This allows SupervisorAgent to use state machines from the StateMachines package without
/// creating a circular dependency.
/// </summary>
/// <typeparam name="TState">The type of the state context.</typeparam>
public class StateMachineAdapter<TState> : ISupervisorStateMachine<TState> where TState : class
{
    private readonly object _stateMachine;
    private readonly Func<object, string?> _getCurrentState;
    private readonly Func<object, string, TState, CancellationToken, Task> _transitionAsync;

    /// <summary>
    /// Initializes a new instance of the <see cref="StateMachineAdapter{TState}"/> class.
    /// </summary>
    /// <param name="stateMachine">The state machine instance (must implement TransitionAsync and have CurrentState property).</param>
    public StateMachineAdapter(object stateMachine)
    {
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));

        // Use reflection to access the state machine methods/properties
        var stateMachineType = stateMachine.GetType();
        var currentStateProperty = stateMachineType.GetProperty("CurrentState");
        var transitionMethod = stateMachineType.GetMethod("TransitionAsync",
            new[] { typeof(string), typeof(TState), typeof(CancellationToken) });

        if (currentStateProperty == null || transitionMethod == null)
        {
            throw new ArgumentException("State machine must have CurrentState property and TransitionAsync method", nameof(stateMachine));
        }

        _getCurrentState = (sm) => currentStateProperty.GetValue(sm) as string;
        _transitionAsync = (sm, toState, context, ct) =>
            (Task)transitionMethod.Invoke(sm, new object[] { toState, context, ct })!;
    }

    /// <inheritdoc/>
    public string? CurrentState => _getCurrentState(_stateMachine);

    /// <inheritdoc/>
    public Task TransitionAsync(string toState, TState context, CancellationToken cancellationToken = default)
    {
        return _transitionAsync(_stateMachine, toState, context, cancellationToken);
    }
}
