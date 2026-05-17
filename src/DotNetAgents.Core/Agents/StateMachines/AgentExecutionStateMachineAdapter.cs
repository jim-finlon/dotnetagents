namespace DotNetAgents.Core.Agents.StateMachines;

/// <summary>
/// Adapter that wraps a state machine instance to implement IAgentExecutionStateMachine.
/// Uses reflection to avoid direct dependency on IStateMachine.
/// </summary>
/// <typeparam name="TState">The type of the state context.</typeparam>
public class AgentExecutionStateMachineAdapter<TState> : IAgentExecutionStateMachine<TState> where TState : class
{
    private readonly object _stateMachine;
    private readonly Func<object, string?> _getCurrentState;
    private readonly Func<object, string, TState, CancellationToken, Task> _transitionAsync;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentExecutionStateMachineAdapter{TState}"/> class.
    /// </summary>
    /// <param name="stateMachine">The state machine instance to wrap.</param>
    /// <exception cref="ArgumentNullException">Thrown when stateMachine is null.</exception>
    /// <exception cref="ArgumentException">Thrown when state machine doesn't have required members.</exception>
    public AgentExecutionStateMachineAdapter(object stateMachine)
    {
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
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

    /// <inheritdoc />
    public string? CurrentState => _getCurrentState(_stateMachine);

    /// <inheritdoc />
    public Task TransitionAsync(string toState, TState context, CancellationToken cancellationToken = default)
    {
        return _transitionAsync(_stateMachine, toState, context, cancellationToken);
    }
}
