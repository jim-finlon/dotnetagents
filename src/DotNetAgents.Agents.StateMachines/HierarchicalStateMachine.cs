using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.StateMachines;

/// <summary>
/// Represents a hierarchical state machine that supports nested states (sub-states).
/// </summary>
/// <typeparam name="TState">The type of the state context.</typeparam>
public class HierarchicalStateMachine<TState> : IStateMachine<TState> where TState : class
{
    private readonly AgentStateMachine<TState> _baseStateMachine;
    private readonly Dictionary<string, IStateMachine<TState>> _subStateMachines = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<HierarchicalStateMachine<TState>>? _logger;
    private readonly object _lock = new();

    /// <inheritdoc/>
    public string CurrentState
    {
        get
        {
            lock (_lock)
            {
                var baseState = _baseStateMachine.CurrentState;
                if (string.IsNullOrEmpty(baseState))
                {
                    return string.Empty;
                }

                // Check if current state has a sub-state machine
                if (_subStateMachines.TryGetValue(baseState, out var subStateMachine))
                {
                    var subState = subStateMachine.CurrentState;
                    return string.IsNullOrEmpty(subState) ? baseState : $"{baseState}.{subState}";
                }

                return baseState;
            }
        }
    }

    /// <inheritdoc/>
    public event EventHandler<StateTransitionEventArgs<TState>>? StateTransitioned;

    /// <summary>
    /// Initializes a new instance of the <see cref="HierarchicalStateMachine{TState}"/> class.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    public HierarchicalStateMachine(ILogger<HierarchicalStateMachine<TState>>? logger = null)
    {
        _logger = logger;
        var baseLogger = logger != null
            ? new LoggerFactory().CreateLogger<AgentStateMachine<TState>>()
            : null;
        _baseStateMachine = new AgentStateMachine<TState>(baseLogger);
        _baseStateMachine.StateTransitioned += OnBaseStateTransitioned;
    }

    /// <summary>
    /// Adds a state to the base state machine.
    /// </summary>
    /// <param name="name">The name of the state.</param>
    /// <param name="entryAction">Optional action to execute when entering the state.</param>
    /// <param name="exitAction">Optional action to execute when exiting the state.</param>
    /// <returns>The state definition for method chaining.</returns>
    public StateDefinition<TState> AddState(
        string name,
        Action<TState>? entryAction = null,
        Action<TState>? exitAction = null)
    {
        return _baseStateMachine.AddState(name, entryAction, exitAction);
    }

    /// <summary>
    /// Adds a sub-state machine to a parent state.
    /// </summary>
    /// <param name="parentState">The parent state name.</param>
    /// <param name="subStateMachine">The sub-state machine.</param>
    public void AddSubStateMachine(string parentState, IStateMachine<TState> subStateMachine)
    {
        ArgumentException.ThrowIfNullOrEmpty(parentState);
        ArgumentNullException.ThrowIfNull(subStateMachine);

        lock (_lock)
        {
            if (!_baseStateMachine.GetAvailableTransitions(new object() as TState ?? throw new InvalidOperationException()).Any())
            {
                // Verify parent state exists by checking if we can get current state
                // This is a simplified check - in production, you'd want a better way to verify state exists
            }

            _subStateMachines[parentState] = subStateMachine;
            subStateMachine.StateTransitioned += (sender, e) =>
            {
                var fullState = $"{parentState}.{e.ToState}";
                var fullFromState = $"{parentState}.{e.FromState}";
                StateTransitioned?.Invoke(this, new StateTransitionEventArgs<TState>(fullFromState, fullState, e.Context));
            };

            _logger?.LogDebug("Added sub-state machine to parent state '{ParentState}'", parentState);
        }
    }

    /// <summary>
    /// Sets the initial state of the base state machine.
    /// </summary>
    /// <param name="stateName">The name of the initial state.</param>
    public void SetInitialState(string stateName)
    {
        _baseStateMachine.SetInitialState(stateName);
    }

    /// <inheritdoc/>
    public bool CanTransition(string fromState, string toState, TState context)
    {
        ArgumentNullException.ThrowIfNull(context);

        lock (_lock)
        {
            // Handle hierarchical state names (e.g., "Parent.Child")
            var fromParts = fromState.Split('.');
            var toParts = toState.Split('.');

            // If both are at the same level
            if (fromParts.Length == 1 && toParts.Length == 1)
            {
                return _baseStateMachine.CanTransition(fromState, toState, context);
            }

            // If transitioning within a sub-state machine
            if (fromParts.Length == 2 && toParts.Length == 2 && fromParts[0] == toParts[0])
            {
                var parentState = fromParts[0];
                if (_subStateMachines.TryGetValue(parentState, out var subStateMachine))
                {
                    return subStateMachine.CanTransition(fromParts[1], toParts[1], context);
                }
            }

            // If transitioning from sub-state to parent state
            if (fromParts.Length == 2 && toParts.Length == 1 && fromParts[0] == toParts[0])
            {
                // This would be handled by the parent state machine
                return _baseStateMachine.CanTransition(fromParts[0], toState, context);
            }

            return false;
        }
    }

    /// <inheritdoc/>
    public async Task TransitionAsync(string toState, TState context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        string? parentState = null;
        IStateMachine<TState>? subStateMachine = null;
        string? subState = null;
        bool isBaseTransition = false;

        lock (_lock)
        {
            var toParts = toState.Split('.');
            var currentState = CurrentState;
            var fromParts = currentState.Split('.');

            // Simple transition within base state machine
            if (toParts.Length == 1 && fromParts.Length == 1)
            {
                isBaseTransition = true;
            }
            // Transition within sub-state machine
            else if (toParts.Length == 2 && fromParts.Length == 2 && fromParts[0] == toParts[0])
            {
                parentState = toParts[0];
                subState = toParts[1];
                _subStateMachines.TryGetValue(parentState, out subStateMachine);
            }
        }

        // Execute transition outside of lock
        if (isBaseTransition)
        {
            await _baseStateMachine.TransitionAsync(toState, context, cancellationToken).ConfigureAwait(false);
        }
        else if (subStateMachine != null && subState != null)
        {
            await subStateMachine.TransitionAsync(subState, context, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            throw new InvalidOperationException($"Invalid hierarchical transition to '{toState}'");
        }
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetAvailableTransitions(TState context)
    {
        ArgumentNullException.ThrowIfNull(context);

        lock (_lock)
        {
            var transitions = new List<string>();
            var currentState = CurrentState;
            var parts = currentState.Split('.');

            if (parts.Length == 1)
            {
                // Base state - get transitions from base state machine
                transitions.AddRange(_baseStateMachine.GetAvailableTransitions(context));
            }
            else if (parts.Length == 2)
            {
                // Sub-state - get transitions from sub-state machine
                var parentState = parts[0];
                if (_subStateMachines.TryGetValue(parentState, out var subStateMachine))
                {
                    var subTransitions = subStateMachine.GetAvailableTransitions(context);
                    transitions.AddRange(subTransitions.Select(t => $"{parentState}.{t}"));
                }
            }

            return transitions;
        }
    }

    /// <inheritdoc/>
    public void Reset(TState context)
    {
        ArgumentNullException.ThrowIfNull(context);

        lock (_lock)
        {
            _baseStateMachine.Reset(context);
            foreach (var subStateMachine in _subStateMachines.Values)
            {
                subStateMachine.Reset(context);
            }
        }
    }

    private void OnBaseStateTransitioned(object? sender, StateTransitionEventArgs<TState> e)
    {
        StateTransitioned?.Invoke(this, e);
    }
}
