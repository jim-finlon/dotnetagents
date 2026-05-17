using DotNetAgents.Agents.StateMachines;

namespace DotNetAgents.Agents.ControlLoops;

/// <summary>
/// Bridge that connects durable workflow execution to a lifecycle state machine.
/// Story 46a0ef9b. Workflow steps publish lifecycle transitions through the
/// bridge; lifecycle state gates whether a workflow may start, continue, retry,
/// or wait. No service ever has to write the same glue twice.
///
/// Additive composition seam — does not modify or replace
/// <see cref="AgentStateMachine{TState}"/> or any workflow engine. Both sides
/// remain independently testable; the bridge just keeps them in sync.
/// </summary>
public sealed class WorkflowLifecycleBridge<TState> where TState : class
{
    private readonly AgentStateMachine<TState> _stateMachine;
    private readonly TState _context;
    private readonly ControlLoopMetricsRecorder? _metrics;

    public WorkflowLifecycleBridge(AgentStateMachine<TState> stateMachine, TState context, ControlLoopMetricsRecorder? metrics = null)
    {
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _metrics = metrics;
    }

    public string CurrentState => _stateMachine.CurrentState;
    public AgentStateMachine<TState> StateMachine => _stateMachine;

    /// <summary>
    /// Publish a lifecycle transition triggered by a workflow step. Returns
    /// true on success, false when the transition is not allowed (the workflow
    /// step should stop / wait / route to recovery). Telemetry is emitted on
    /// success.
    /// </summary>
    public async Task<bool> TransitionToAsync(string newState, string? reason = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(newState)) throw new ArgumentException("newState is required.", nameof(newState));
        var fromState = _stateMachine.CurrentState;
        if (!_stateMachine.CanTransition(fromState, newState, _context)) return false;
        await _stateMachine.TransitionAsync(newState, _context, ct).ConfigureAwait(false);
        _metrics?.Transition(fromState, newState);
        return true;
    }

    /// <summary>
    /// Standard mappings from workflow lifecycle moments to LifecyclePatterns
    /// state names. Workflow steps call these instead of remembering string
    /// constants. Returns true when the transition was allowed and applied.
    /// </summary>
    public Task<bool> MarkRunningAsync(string? reason = null, CancellationToken ct = default) =>
        TransitionToAsync(LifecyclePatterns.States.Running, reason, ct);

    public Task<bool> MarkWaitingAsync(string? reason = null, CancellationToken ct = default) =>
        TransitionToAsync(LifecyclePatterns.States.Waiting, reason, ct);

    public Task<bool> MarkBlockedAsync(string? reason = null, CancellationToken ct = default) =>
        TransitionToAsync(LifecyclePatterns.States.Blocked, reason, ct);

    public Task<bool> MarkDegradedAsync(string? reason = null, CancellationToken ct = default) =>
        TransitionToAsync(LifecyclePatterns.States.Degraded, reason, ct);

    public Task<bool> MarkRecoveringAsync(string? reason = null, CancellationToken ct = default) =>
        TransitionToAsync(LifecyclePatterns.States.Recovering, reason, ct);

    public Task<bool> MarkCompletedAsync(string? reason = null, CancellationToken ct = default) =>
        TransitionToAsync(LifecyclePatterns.States.Completed, reason, ct);

    public Task<bool> MarkCoolingDownAsync(string? reason = null, CancellationToken ct = default) =>
        TransitionToAsync(LifecyclePatterns.States.CoolingDown, reason, ct);

    /// <summary>
    /// Lifecycle gate. Returns the verdict for whether a workflow operation is
    /// allowed in the current state. Use the gate before kicking off a step,
    /// retrying, or resuming after a wait.
    /// </summary>
    public WorkflowGateVerdict GateOperation(WorkflowOperation op)
    {
        var current = _stateMachine.CurrentState;
        var allowed = (op, current) switch
        {
            // Start: allowed only from Idle / CoolingDown / Sampling (evolutionary)
            (WorkflowOperation.Start, LifecyclePatterns.States.Idle) => true,
            (WorkflowOperation.Start, LifecyclePatterns.States.CoolingDown) => true,
            (WorkflowOperation.Start, LifecyclePatterns.States.Sampling) => true,

            // Continue: allowed from Running / Recovering
            (WorkflowOperation.Continue, LifecyclePatterns.States.Running) => true,
            (WorkflowOperation.Continue, LifecyclePatterns.States.Recovering) => true,
            (WorkflowOperation.Continue, LifecyclePatterns.States.Training) => true,
            (WorkflowOperation.Continue, LifecyclePatterns.States.Evaluating) => true,

            // Retry: allowed from Degraded / Recovering / Blocked
            (WorkflowOperation.Retry, LifecyclePatterns.States.Degraded) => true,
            (WorkflowOperation.Retry, LifecyclePatterns.States.Recovering) => true,
            (WorkflowOperation.Retry, LifecyclePatterns.States.Blocked) => true,

            // Resume: allowed from Waiting / Blocked
            (WorkflowOperation.Resume, LifecyclePatterns.States.Waiting) => true,
            (WorkflowOperation.Resume, LifecyclePatterns.States.Blocked) => true,

            _ => false,
        };

        return allowed
            ? new WorkflowGateVerdict(true, current, op, $"{op} permitted from {current}")
            : new WorkflowGateVerdict(false, current, op, $"{op} not permitted from state {current}");
    }
}

public enum WorkflowOperation { Start, Continue, Retry, Resume }

public sealed record WorkflowGateVerdict(bool Allowed, string CurrentState, WorkflowOperation Operation, string Reason);
