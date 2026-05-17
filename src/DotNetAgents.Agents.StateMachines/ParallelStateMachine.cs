using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.StateMachines;

/// <summary>
/// Represents a state machine with parallel orthogonal regions (parallel states).
/// </summary>
/// <typeparam name="TState">The type of the state context.</typeparam>
public class ParallelStateMachine<TState> : IStateMachine<TState> where TState : class
{
    private readonly Dictionary<string, IStateMachine<TState>> _regions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<ParallelStateMachine<TState>>? _logger;
    private readonly object _lock = new();

    /// <inheritdoc/>
    public string CurrentState
    {
        get
        {
            lock (_lock)
            {
                var states = _regions.Values
                    .Select(sm => sm.CurrentState)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();

                return states.Count > 0
                    ? string.Join("|", states)
                    : string.Empty;
            }
        }
    }

    /// <inheritdoc/>
    public event EventHandler<StateTransitionEventArgs<TState>>? StateTransitioned;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParallelStateMachine{TState}"/> class.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    public ParallelStateMachine(ILogger<ParallelStateMachine<TState>>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adds a parallel region to the state machine.
    /// </summary>
    /// <param name="regionName">The name of the region.</param>
    /// <param name="stateMachine">The state machine for this region.</param>
    public void AddRegion(string regionName, IStateMachine<TState> stateMachine)
    {
        ArgumentException.ThrowIfNullOrEmpty(regionName);
        ArgumentNullException.ThrowIfNull(stateMachine);

        lock (_lock)
        {
            _regions[regionName] = stateMachine;
            stateMachine.StateTransitioned += (sender, e) =>
            {
                StateTransitioned?.Invoke(this, new StateTransitionEventArgs<TState>(
                    $"{regionName}:{e.FromState}",
                    $"{regionName}:{e.ToState}",
                    e.Context));
            };

            _logger?.LogDebug("Added parallel region '{RegionName}'", regionName);
        }
    }

    /// <inheritdoc/>
    public bool CanTransition(string fromState, string toState, TState context)
    {
        ArgumentNullException.ThrowIfNull(context);

        lock (_lock)
        {
            // Parse region:state format
            var fromParts = fromState.Split(':');
            var toParts = toState.Split(':');

            if (fromParts.Length != 2 || toParts.Length != 2)
            {
                return false;
            }

            if (fromParts[0] != toParts[0])
            {
                return false; // Can't transition between regions
            }

            var regionName = fromParts[0];
            if (_regions.TryGetValue(regionName, out var region))
            {
                return region.CanTransition(fromParts[1], toParts[1], context);
            }

            return false;
        }
    }

    /// <inheritdoc/>
    public async Task TransitionAsync(string toState, TState context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        IStateMachine<TState>? region = null;
        string? targetState = null;

        lock (_lock)
        {
            var parts = toState.Split(':');
            if (parts.Length != 2)
            {
                throw new InvalidOperationException($"Invalid parallel state format: '{toState}'. Expected 'Region:State'.");
            }

            var regionName = parts[0];
            if (!_regions.TryGetValue(regionName, out region))
            {
                throw new InvalidOperationException($"Region '{regionName}' does not exist.");
            }

            targetState = parts[1];
        }

        // Execute transition outside of lock
        if (region != null && targetState != null)
        {
            await region.TransitionAsync(targetState, context, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetAvailableTransitions(TState context)
    {
        ArgumentNullException.ThrowIfNull(context);

        lock (_lock)
        {
            var transitions = new List<string>();
            foreach (var kvp in _regions)
            {
                var regionTransitions = kvp.Value.GetAvailableTransitions(context);
                transitions.AddRange(regionTransitions.Select(t => $"{kvp.Key}:{t}"));
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
            foreach (var region in _regions.Values)
            {
                region.Reset(context);
            }
        }
    }
}
