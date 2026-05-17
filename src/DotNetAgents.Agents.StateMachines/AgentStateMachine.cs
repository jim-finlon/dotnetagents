using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace DotNetAgents.Agents.StateMachines;

/// <summary>
/// Default implementation of <see cref="IStateMachine{TState}"/> for agent state management.
/// </summary>
/// <typeparam name="TState">The type of the state context.</typeparam>
public class AgentStateMachine<TState> : IStateMachine<TState> where TState : class
{
    private readonly Dictionary<string, StateDefinition<TState>> _states = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<StateTransition<TState>>> _transitions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<TimedStateTransition<TState>>> _timedTransitions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<ScheduledStateTransition<TState>>> _scheduledTransitions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTimeOffset> _stateEntryTimes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<AgentStateMachine<TState>>? _logger;
    private readonly IStateMachinePersistence<TState>? _persistence;
    private readonly Meter? _meter;
    private readonly Counter<long>? _transitionCounter;
    private readonly Histogram<double>? _transitionDuration;
    private readonly object _lock = new();
    private readonly StateTransitionScheduler<TState> _transitionScheduler;
    private string? _initialState;
    private string? _currentState;
    private readonly List<StateTransitionHistory<TState>> _transitionHistory = new();
    private int _maxHistorySize = 100;

    /// <inheritdoc/>
    public string CurrentState
    {
        get
        {
            lock (_lock)
            {
                return _currentState ?? _initialState ?? string.Empty;
            }
        }
    }

    /// <inheritdoc/>
    public event EventHandler<StateTransitionEventArgs<TState>>? StateTransitioned;

    /// <summary>
    /// Gets or sets the maximum number of transition history entries to keep.
    /// </summary>
    public int MaxHistorySize
    {
        get => _maxHistorySize;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "MaxHistorySize cannot be negative.");
            }
            _maxHistorySize = value;
        }
    }

    /// <summary>
    /// Gets the transition history.
    /// </summary>
    public IReadOnlyList<StateTransitionHistory<TState>> TransitionHistory
    {
        get
        {
            lock (_lock)
            {
                return _transitionHistory.ToList().AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentStateMachine{TState}"/> class.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    /// <param name="persistence">Optional persistence provider for state machine state.</param>
    /// <param name="meter">Optional meter for metrics collection.</param>
    public AgentStateMachine(
        ILogger<AgentStateMachine<TState>>? logger = null,
        IStateMachinePersistence<TState>? persistence = null,
        Meter? meter = null)
    {
        _logger = logger;
        _persistence = persistence;
        _meter = meter ?? new Meter(StateMachineActivitySource.SourceName);

        _transitionCounter = _meter.CreateCounter<long>(
            "state_transitions_total",
            "count",
            "Total number of state transitions");

        _transitionDuration = _meter.CreateHistogram<double>(
            "state_transition_duration_seconds",
            "seconds",
            "Duration of state transitions");
        _transitionScheduler = new StateTransitionScheduler<TState>(
            _lock,
            () => _currentState,
            TransitionAsync,
            _logger);
    }

    /// <summary>
    /// Adds a state to the state machine.
    /// </summary>
    /// <param name="name">The name of the state.</param>
    /// <param name="entryAction">Optional action to execute when entering the state.</param>
    /// <param name="exitAction">Optional action to execute when exiting the state.</param>
    /// <param name="entryActionAsync">Optional asynchronous action to execute when entering the state.</param>
    /// <param name="exitActionAsync">Optional asynchronous action to execute when exiting the state.</param>
    /// <returns>The state definition for method chaining.</returns>
    public StateDefinition<TState> AddState(
        string name,
        Action<TState>? entryAction = null,
        Action<TState>? exitAction = null,
        Func<TState, CancellationToken, Task>? entryActionAsync = null,
        Func<TState, CancellationToken, Task>? exitActionAsync = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name), "State name cannot be null or empty.");
        }

        lock (_lock)
        {
            if (_states.ContainsKey(name))
            {
                throw new InvalidOperationException($"State '{name}' already exists.");
            }

            var stateDefinition = new StateDefinition<TState>(name)
            {
                EntryAction = entryAction,
                ExitAction = exitAction,
                EntryActionAsync = entryActionAsync,
                ExitActionAsync = exitActionAsync
            };

            _states[name] = stateDefinition;
            _transitions[name] = new List<StateTransition<TState>>();

            _logger?.LogDebug("Added state '{StateName}' to state machine", name);
        }

        return _states[name];
    }

    /// <summary>
    /// Adds a transition between states.
    /// </summary>
    /// <param name="fromState">The source state.</param>
    /// <param name="toState">The target state.</param>
    /// <param name="guard">Optional guard condition.</param>
    /// <param name="onTransition">Optional transition action.</param>
    /// <param name="onTransitionAsync">Optional asynchronous transition action.</param>
    public void AddTransition(
        string fromState,
        string toState,
        Func<TState, bool>? guard = null,
        Action<TState>? onTransition = null,
        Func<TState, CancellationToken, Task>? onTransitionAsync = null)
    {
        if (string.IsNullOrWhiteSpace(fromState))
        {
            throw new ArgumentNullException(nameof(fromState), "From state cannot be null or empty.");
        }

        if (string.IsNullOrWhiteSpace(toState))
        {
            throw new ArgumentNullException(nameof(toState), "To state cannot be null or empty.");
        }

        lock (_lock)
        {
            if (!_states.ContainsKey(fromState))
            {
                throw new InvalidOperationException($"Source state '{fromState}' does not exist.");
            }

            if (!_states.ContainsKey(toState))
            {
                throw new InvalidOperationException($"Target state '{toState}' does not exist.");
            }

            var transition = new StateTransition<TState>(
                fromState,
                toState,
                guard,
                onTransition,
                onTransitionAsync);

            _transitions[fromState].Add(transition);

            _logger?.LogDebug("Added transition from '{FromState}' to '{ToState}'", fromState, toState);
        }
    }

    /// <summary>
    /// Adds a timeout transition that automatically transitions after a specified duration.
    /// </summary>
    /// <param name="fromState">The source state.</param>
    /// <param name="toState">The target state.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <param name="onTimeout">Optional action to execute on timeout.</param>
    /// <param name="onTimeoutAsync">Optional asynchronous action to execute on timeout.</param>
    public void AddTimeoutTransition(
        string fromState,
        string toState,
        TimeSpan timeout,
        Action<TState>? onTimeout = null,
        Func<TState, CancellationToken, Task>? onTimeoutAsync = null)
    {
        if (string.IsNullOrWhiteSpace(fromState))
        {
            throw new ArgumentNullException(nameof(fromState), "From state cannot be null or empty.");
        }

        if (string.IsNullOrWhiteSpace(toState))
        {
            throw new ArgumentNullException(nameof(toState), "To state cannot be null or empty.");
        }

        lock (_lock)
        {
            if (!_states.ContainsKey(fromState))
            {
                throw new InvalidOperationException($"Source state '{fromState}' does not exist.");
            }

            if (!_states.ContainsKey(toState))
            {
                throw new InvalidOperationException($"Target state '{toState}' does not exist.");
            }

            var timedTransition = new TimedStateTransition<TState>(
                fromState,
                toState,
                timeout,
                onTimeout,
                onTimeoutAsync);

            if (!_timedTransitions.ContainsKey(fromState))
            {
                _timedTransitions[fromState] = new List<TimedStateTransition<TState>>();
            }

            _timedTransitions[fromState].Add(timedTransition);

            _logger?.LogDebug("Added timeout transition from '{FromState}' to '{ToState}' after {Timeout}", fromState, toState, timeout);
        }
    }

    /// <summary>
    /// Adds a scheduled transition that automatically transitions at a specific time.
    /// </summary>
    /// <param name="fromState">The source state.</param>
    /// <param name="toState">The target state.</param>
    /// <param name="scheduledTime">The scheduled time for the transition.</param>
    /// <param name="onScheduled">Optional action to execute on scheduled time.</param>
    /// <param name="onScheduledAsync">Optional asynchronous action to execute on scheduled time.</param>
    public void AddScheduledTransition(
        string fromState,
        string toState,
        DateTimeOffset scheduledTime,
        Action<TState>? onScheduled = null,
        Func<TState, CancellationToken, Task>? onScheduledAsync = null)
    {
        if (string.IsNullOrWhiteSpace(fromState))
        {
            throw new ArgumentNullException(nameof(fromState), "From state cannot be null or empty.");
        }

        if (string.IsNullOrWhiteSpace(toState))
        {
            throw new ArgumentNullException(nameof(toState), "To state cannot be null or empty.");
        }

        lock (_lock)
        {
            if (!_states.ContainsKey(fromState))
            {
                throw new InvalidOperationException($"Source state '{fromState}' does not exist.");
            }

            if (!_states.ContainsKey(toState))
            {
                throw new InvalidOperationException($"Target state '{toState}' does not exist.");
            }

            var scheduledTransition = new ScheduledStateTransition<TState>(
                fromState,
                toState,
                scheduledTime,
                onScheduled,
                onScheduledAsync);

            if (!_scheduledTransitions.ContainsKey(fromState))
            {
                _scheduledTransitions[fromState] = new List<ScheduledStateTransition<TState>>();
            }

            _scheduledTransitions[fromState].Add(scheduledTransition);

            _logger?.LogDebug("Added scheduled transition from '{FromState}' to '{ToState}' at {ScheduledTime}",
                fromState, toState, scheduledTime);
        }
    }

    /// <summary>
    /// Gets the duration the current state has been active.
    /// </summary>
    /// <returns>The duration, or null if the state entry time is not tracked.</returns>
    public TimeSpan? GetStateDuration()
    {
        lock (_lock)
        {
            var currentState = _currentState ?? _initialState;
            if (string.IsNullOrEmpty(currentState) || !_stateEntryTimes.TryGetValue(currentState, out var entryTime))
            {
                return null;
            }

            return DateTimeOffset.UtcNow - entryTime;
        }
    }

    /// <summary>
    /// Sets the initial state of the state machine.
    /// </summary>
    /// <param name="stateName">The name of the initial state.</param>
    public void SetInitialState(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName))
        {
            throw new ArgumentNullException(nameof(stateName), "Initial state name cannot be null or empty.");
        }

        lock (_lock)
        {
            if (!_states.ContainsKey(stateName))
            {
                throw new InvalidOperationException($"Initial state '{stateName}' does not exist.");
            }

            _initialState = stateName;
            _currentState = stateName;

            _logger?.LogDebug("Set initial state to '{StateName}'", stateName);
        }
    }

    /// <inheritdoc/>
    public bool CanTransition(string fromState, string toState, TState context)
    {
        ArgumentNullException.ThrowIfNull(context);

        lock (_lock)
        {
            if (!_transitions.TryGetValue(fromState, out var transitions))
            {
                return false;
            }

            var transition = transitions.FirstOrDefault(t => t.ToState.Equals(toState, StringComparison.OrdinalIgnoreCase));
            if (transition == null)
            {
                return false;
            }

            // Check guard condition if present
            if (transition.Guard != null)
            {
                try
                {
                    return transition.Guard(context);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error evaluating guard condition for transition from '{FromState}' to '{ToState}'", fromState, toState);
                    return false;
                }
            }

            return true;
        }
    }

    /// <inheritdoc/>
    public async Task TransitionAsync(string toState, TState context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(toState))
        {
            throw new ArgumentNullException(nameof(toState), "Target state cannot be null or empty.");
        }

        string fromState;
        StateDefinition<TState>? fromStateDefinition;
        StateDefinition<TState>? toStateDefinition;
        StateTransition<TState>? transition;

        lock (_lock)
        {
            fromState = _currentState ?? _initialState ?? throw new InvalidOperationException("State machine has no initial state set.");

            if (!_states.TryGetValue(fromState, out fromStateDefinition))
            {
                throw new InvalidOperationException($"Current state '{fromState}' does not exist.");
            }

            if (!_states.TryGetValue(toState, out toStateDefinition))
            {
                throw new InvalidOperationException($"Target state '{toState}' does not exist.");
            }

            if (!CanTransition(fromState, toState, context))
            {
                throw new InvalidOperationException($"Transition from '{fromState}' to '{toState}' is not allowed.");
            }

            transition = _transitions[fromState].FirstOrDefault(t => t.ToState.Equals(toState, StringComparison.OrdinalIgnoreCase));
        }

        _logger?.LogInformation("Transitioning from '{FromState}' to '{ToState}'", fromState, toState);

        // Start tracing activity
        using var activity = StateMachineActivitySource.Source.StartActivity(
            StateMachineActivitySource.TransitionActivityName,
            ActivityKind.Internal);

        activity?.SetTag("state_machine.from_state", fromState);
        activity?.SetTag("state_machine.to_state", toState);

        var stopwatch = Stopwatch.StartNew();

        // Execute exit action of current state
        try
        {
            fromStateDefinition.ExitAction?.Invoke(context);
            if (fromStateDefinition.ExitActionAsync != null)
            {
                await fromStateDefinition.ExitActionAsync(context, cancellationToken).ConfigureAwait(false);
            }
            fromStateDefinition.RaiseOnExit(context);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing exit action for state '{StateName}'", fromState);
            throw;
        }

        // Execute transition action
        try
        {
            transition?.OnTransition?.Invoke(context);
            if (transition?.OnTransitionAsync != null)
            {
                await transition.OnTransitionAsync(context, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing transition action from '{FromState}' to '{ToState}'", fromState, toState);
            throw;
        }

        // Update current state and track entry time
        lock (_lock)
        {
            if (_currentState != null)
            {
                _transitionScheduler.CancelTimeoutForState(_currentState);
            }

            _currentState = toState;
            _stateEntryTimes[toState] = DateTimeOffset.UtcNow;

            // Start timeout transitions for the new state
            if (_timedTransitions.TryGetValue(toState, out var timedTransitions))
            {
                foreach (var timedTransition in timedTransitions)
                {
                    _transitionScheduler.StartTimeoutTransition(timedTransition, context, cancellationToken);
                }
            }

            // Start scheduled transitions for the new state
            if (_scheduledTransitions.TryGetValue(toState, out var scheduledTransitions))
            {
                foreach (var scheduledTransition in scheduledTransitions)
                {
                    _transitionScheduler.StartScheduledTransition(scheduledTransition, context, cancellationToken);
                }
            }
        }

        // Execute entry action of new state
        try
        {
            toStateDefinition.EntryAction?.Invoke(context);
            if (toStateDefinition.EntryActionAsync != null)
            {
                await toStateDefinition.EntryActionAsync(context, cancellationToken).ConfigureAwait(false);
            }
            toStateDefinition.RaiseOnEntry(context);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing entry action for state '{StateName}'", toState);
            throw;
        }

        // Record transition in history
        lock (_lock)
        {
            _transitionHistory.Add(new StateTransitionHistory<TState>(fromState, toState));

            // Trim history if it exceeds max size
            if (_transitionHistory.Count > _maxHistorySize)
            {
                _transitionHistory.RemoveAt(0);
            }
        }

        // Save state if persistence is configured
        if (_persistence != null)
        {
            try
            {
                // Use a machine ID if available, otherwise use a default
                var machineId = GetMachineId(context);
                await _persistence.SaveStateAsync(machineId, toState, context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to persist state machine state");
                // Don't throw - persistence failure shouldn't break the transition
            }
        }

        // Record metrics
        stopwatch.Stop();
        _transitionCounter?.Add(1, new KeyValuePair<string, object?>("from_state", fromState), new KeyValuePair<string, object?>("to_state", toState));
        _transitionDuration?.Record(stopwatch.Elapsed.TotalSeconds, new KeyValuePair<string, object?>("from_state", fromState), new KeyValuePair<string, object?>("to_state", toState));

        activity?.SetTag("state_machine.duration_ms", stopwatch.ElapsedMilliseconds);
        activity?.SetStatus(ActivityStatusCode.Ok);

        // Raise transition event
        var eventArgs = new StateTransitionEventArgs<TState>(fromState, toState, context);
        StateTransitioned?.Invoke(this, eventArgs);

        _logger?.LogInformation("Successfully transitioned from '{FromState}' to '{ToState}' in {Duration}ms", fromState, toState, stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Gets a machine identifier from the context.
    /// Override this method in derived classes to provide custom machine ID logic.
    /// </summary>
    /// <param name="context">The state context.</param>
    /// <returns>A machine identifier.</returns>
    protected virtual string GetMachineId(TState context)
    {
        // Default implementation - can be overridden
        return context.GetHashCode().ToString();
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetAvailableTransitions(TState context)
    {
        ArgumentNullException.ThrowIfNull(context);

        lock (_lock)
        {
            var currentState = _currentState ?? _initialState;
            if (string.IsNullOrEmpty(currentState) || !_transitions.TryGetValue(currentState, out var transitions))
            {
                return Enumerable.Empty<string>();
            }

            return transitions
                .Where(t => t.Guard == null || t.Guard(context))
                .Select(t => t.ToState)
                .Distinct()
                .ToList();
        }
    }

    /// <inheritdoc/>
    public void Reset(TState context)
    {
        ArgumentNullException.ThrowIfNull(context);

        lock (_lock)
        {
            if (string.IsNullOrEmpty(_initialState))
            {
                throw new InvalidOperationException("Cannot reset: no initial state is set.");
            }

            _currentState = _initialState;

            if (_states.TryGetValue(_initialState, out var initialStateDefinition))
            {
                try
                {
                    initialStateDefinition.EntryAction?.Invoke(context);
                    initialStateDefinition.RaiseOnEntry(context);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error executing entry action for initial state '{StateName}'", _initialState);
                }
            }

            _logger?.LogInformation("State machine reset to initial state '{StateName}'", _initialState);
        }
    }
}
