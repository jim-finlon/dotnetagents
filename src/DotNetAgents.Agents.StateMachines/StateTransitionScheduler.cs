// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.StateMachines;

internal sealed class StateTransitionScheduler<TState> where TState : class
{
    private readonly Dictionary<string, CancellationTokenSource> _timeoutCancellationSources = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CancellationTokenSource> _scheduledCancellationSources = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _syncRoot;
    private readonly Func<string?> _currentStateProvider;
    private readonly Func<string, TState, CancellationToken, Task> _transitionAsync;
    private readonly ILogger? _logger;

    public StateTransitionScheduler(
        object syncRoot,
        Func<string?> currentStateProvider,
        Func<string, TState, CancellationToken, Task> transitionAsync,
        ILogger? logger)
    {
        _syncRoot = syncRoot;
        _currentStateProvider = currentStateProvider;
        _transitionAsync = transitionAsync;
        _logger = logger;
    }

    public void CancelTimeoutForState(string state)
    {
        lock (_syncRoot)
        {
            if (_timeoutCancellationSources.TryGetValue(state, out var oldCts))
            {
                oldCts.Cancel();
                _timeoutCancellationSources.Remove(state);
            }
        }
    }

    public void StartTimeoutTransition(
        TimedStateTransition<TState> timedTransition,
        TState context,
        CancellationToken cancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        lock (_syncRoot)
        {
            _timeoutCancellationSources[timedTransition.FromState] = cts;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(timedTransition.Timeout, cts.Token).ConfigureAwait(false);

                if (!IsCurrentState(timedTransition.FromState))
                {
                    return;
                }

                timedTransition.OnTimeout?.Invoke(context);
                if (timedTransition.OnTimeoutAsync != null)
                {
                    await timedTransition.OnTimeoutAsync(context, cancellationToken).ConfigureAwait(false);
                }

                await _transitionAsync(timedTransition.ToState, context, cancellationToken).ConfigureAwait(false);

                _logger?.LogInformation("Timeout transition triggered from '{FromState}' to '{ToState}'",
                    timedTransition.FromState, timedTransition.ToState);
            }
            catch (OperationCanceledException)
            {
                _logger?.LogDebug("Timeout transition cancelled for state '{State}'", timedTransition.FromState);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing timeout transition from '{FromState}' to '{ToState}'",
                    timedTransition.FromState, timedTransition.ToState);
            }
            finally
            {
                lock (_syncRoot)
                {
                    _timeoutCancellationSources.Remove(timedTransition.FromState);
                }
            }
        }, cts.Token);
    }

    public void StartScheduledTransition(
        ScheduledStateTransition<TState> scheduledTransition,
        TState context,
        CancellationToken cancellationToken)
    {
        var delay = scheduledTransition.ScheduledTime - DateTimeOffset.UtcNow;
        if (delay <= TimeSpan.Zero)
        {
            StartElapsedScheduledTransition(scheduledTransition, context, cancellationToken);
            return;
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var key = GetScheduledKey(scheduledTransition);
        lock (_syncRoot)
        {
            _scheduledCancellationSources[key] = cts;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay, cts.Token).ConfigureAwait(false);

                if (!IsCurrentState(scheduledTransition.FromState))
                {
                    return;
                }

                scheduledTransition.OnScheduled?.Invoke(context);
                if (scheduledTransition.OnScheduledAsync != null)
                {
                    await scheduledTransition.OnScheduledAsync(context, cancellationToken).ConfigureAwait(false);
                }

                await _transitionAsync(scheduledTransition.ToState, context, cancellationToken).ConfigureAwait(false);

                _logger?.LogInformation("Scheduled transition triggered from '{FromState}' to '{ToState}'",
                    scheduledTransition.FromState, scheduledTransition.ToState);
            }
            catch (OperationCanceledException)
            {
                _logger?.LogDebug("Scheduled transition cancelled for state '{State}'", scheduledTransition.FromState);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing scheduled transition from '{FromState}' to '{ToState}'",
                    scheduledTransition.FromState, scheduledTransition.ToState);
            }
            finally
            {
                lock (_syncRoot)
                {
                    _scheduledCancellationSources.Remove(key);
                }
            }
        }, cts.Token);
    }

    private void StartElapsedScheduledTransition(
        ScheduledStateTransition<TState> scheduledTransition,
        TState context,
        CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                scheduledTransition.OnScheduled?.Invoke(context);
                if (scheduledTransition.OnScheduledAsync != null)
                {
                    await scheduledTransition.OnScheduledAsync(context, cancellationToken).ConfigureAwait(false);
                }

                await _transitionAsync(scheduledTransition.ToState, context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing scheduled transition from '{FromState}' to '{ToState}'",
                    scheduledTransition.FromState, scheduledTransition.ToState);
            }
        }, cancellationToken);
    }

    private bool IsCurrentState(string state)
    {
        lock (_syncRoot)
        {
            return _currentStateProvider() == state;
        }
    }

    private static string GetScheduledKey(ScheduledStateTransition<TState> scheduledTransition)
        => $"{scheduledTransition.FromState}:{scheduledTransition.ScheduledTime:O}";
}
