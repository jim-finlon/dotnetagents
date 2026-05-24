// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Workflow.PromptRouting;

public interface IPromptRoutePromptResolver
{
    Task<string?> ResolvePromptAsync(string promptId, CancellationToken cancellationToken = default);
}

public sealed class InMemoryPromptRoutePromptResolver : IPromptRoutePromptResolver
{
    private readonly IReadOnlyDictionary<string, string> _prompts;

    public InMemoryPromptRoutePromptResolver(IReadOnlyDictionary<string, string> prompts) =>
        _prompts = prompts;

    public Task<string?> ResolvePromptAsync(string promptId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptId);
        return Task.FromResult(_prompts.TryGetValue(promptId, out var prompt) ? prompt : null);
    }
}

public sealed record PromptRouteSessionState(
    string SessionId,
    string MachineId,
    string CurrentStateId,
    IReadOnlyList<PromptRouteTransitionTrace> Trace);

public sealed record PromptRouteTransitionTrace(
    DateTimeOffset OccurredAt,
    string MachineId,
    string FromStateId,
    string? ToStateId,
    string? TriggerLabel,
    PromptRouteTransitionStatus Status,
    string Reason,
    IReadOnlyList<string> EvidenceRefs);

public interface IPromptRouteSessionStateStore
{
    Task<PromptRouteSessionState?> GetAsync(string sessionId, string machineId, CancellationToken cancellationToken = default);

    Task SaveAsync(PromptRouteSessionState state, CancellationToken cancellationToken = default);
}

public sealed class InMemoryPromptRouteSessionStateStore : IPromptRouteSessionStateStore
{
    private readonly Dictionary<(string SessionId, string MachineId), PromptRouteSessionState> _states = new();

    public Task<PromptRouteSessionState?> GetAsync(string sessionId, string machineId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(machineId);
        lock (_states)
        {
            _states.TryGetValue((sessionId, machineId), out var state);
            return Task.FromResult(state);
        }
    }

    public Task SaveAsync(PromptRouteSessionState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        lock (_states)
        {
            _states[(state.SessionId, state.MachineId)] = state;
        }

        return Task.CompletedTask;
    }
}

public sealed record PromptRouteResolvedStep(
    PromptRouteStep Step,
    string CurrentStateId,
    string? SelectedPromptBody,
    string? ExitPromptBody,
    IReadOnlyList<PromptRouteTransitionTrace> Trace);

public sealed class PromptRouteLiveLoop
{
    private const int MaxTraceEntries = 10;
    private readonly IPromptRouteSessionStateStore _stateStore;
    private readonly IPromptRoutePromptResolver _promptResolver;
    private readonly PromptRouteBlockedRepeatDetector _repeatDetector;

    public PromptRouteLiveLoop(
        IPromptRouteSessionStateStore stateStore,
        IPromptRoutePromptResolver promptResolver,
        PromptRouteBlockedRepeatDetector? repeatDetector = null)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _promptResolver = promptResolver ?? throw new ArgumentNullException(nameof(promptResolver));
        _repeatDetector = repeatDetector ?? new PromptRouteBlockedRepeatDetector();
    }

    public async Task<PromptRouteResolvedStep> TransitionAsync(
        string sessionId,
        PromptRouteMachine machine,
        string triggerLabel,
        PromptRouteContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(machine);

        var prior = await GetOrCreateAsync(sessionId, machine, cancellationToken).ConfigureAwait(false);
        var step = PromptRouteRunner.Transition(machine, prior.CurrentStateId, triggerLabel, context);
        var currentStateId = step.Status == PromptRouteTransitionStatus.Transitioned && !string.IsNullOrWhiteSpace(step.ToStateId)
            ? step.ToStateId!
            : prior.CurrentStateId;

        return await PersistAndResolveAsync(sessionId, machine, prior, step, currentStateId, triggerLabel, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PromptRouteResolvedStep> ResetAsync(
        string sessionId,
        PromptRouteMachine machine,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(machine);

        var prior = await GetOrCreateAsync(sessionId, machine, cancellationToken).ConfigureAwait(false);
        var step = PromptRouteRunner.Reset(machine, prior.CurrentStateId);
        var currentStateId = string.IsNullOrWhiteSpace(step.ToStateId) ? machine.InitialStateId : step.ToStateId!;
        return await PersistAndResolveAsync(sessionId, machine, prior, step, currentStateId, "reset", cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<PromptRouteSessionState> GetOrCreateAsync(
        string sessionId,
        PromptRouteMachine machine,
        CancellationToken cancellationToken)
    {
        var state = await _stateStore.GetAsync(sessionId, machine.MachineId, cancellationToken).ConfigureAwait(false);
        return state ?? new PromptRouteSessionState(sessionId, machine.MachineId, machine.InitialStateId, Array.Empty<PromptRouteTransitionTrace>());
    }

    private async Task<PromptRouteResolvedStep> PersistAndResolveAsync(
        string sessionId,
        PromptRouteMachine machine,
        PromptRouteSessionState prior,
        PromptRouteStep step,
        string currentStateId,
        string? triggerLabel,
        CancellationToken cancellationToken)
    {
        var trace = new PromptRouteTransitionTrace(
            DateTimeOffset.UtcNow,
            machine.MachineId,
            step.FromStateId,
            step.ToStateId,
            triggerLabel,
            step.Status,
            step.Reason,
            step.EvidenceRefs);

        var traces = prior.Trace.Concat(new[] { trace }).TakeLast(MaxTraceEntries).ToList();
        await _stateStore.SaveAsync(new PromptRouteSessionState(sessionId, machine.MachineId, currentStateId, traces), cancellationToken)
            .ConfigureAwait(false);
        await _repeatDetector.ObserveAsync(trace, cancellationToken).ConfigureAwait(false);

        var selected = await ResolveWithFallbackAsync(step.SelectedPromptId, step.SelectedPrompt, cancellationToken).ConfigureAwait(false);
        var exit = await ResolveWithFallbackAsync(step.ExitPromptId, step.ExitPrompt, cancellationToken).ConfigureAwait(false);
        return new PromptRouteResolvedStep(step, currentStateId, selected, exit, traces);
    }

    private async Task<string?> ResolveWithFallbackAsync(string? promptId, string? fallback, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(promptId))
            return fallback;

        return await _promptResolver.ResolvePromptAsync(promptId, cancellationToken).ConfigureAwait(false) ?? fallback;
    }
}

public interface IPromptRouteLessonEmitter
{
    Task EmitRepeatBlockLessonAsync(PromptRouteRepeatBlockObservation observation, CancellationToken cancellationToken = default);
}

public sealed record PromptRouteRepeatBlockObservation(
    string MachineId,
    string TransitionKey,
    PromptRouteTransitionStatus Status,
    int Count,
    string ProblemSignature);

public sealed class PromptRouteBlockedRepeatDetector
{
    private readonly Dictionary<(string MachineId, string TransitionKey, PromptRouteTransitionStatus Status), int> _counts = new();
    private readonly IPromptRouteLessonEmitter? _lessonEmitter;
    private readonly int _threshold;

    public PromptRouteBlockedRepeatDetector(IPromptRouteLessonEmitter? lessonEmitter = null, int threshold = 3)
    {
        if (threshold < 2)
            throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be at least 2.");

        _lessonEmitter = lessonEmitter;
        _threshold = threshold;
    }

    public async Task<PromptRouteRepeatBlockObservation?> ObserveAsync(
        PromptRouteTransitionTrace trace,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(trace);
        if (trace.Status is not (PromptRouteTransitionStatus.GuardUnsatisfied or PromptRouteTransitionStatus.SafetyGateRefused))
            return null;

        var transitionKey = $"{trace.FromStateId}->{trace.ToStateId ?? "blocked"}:{trace.TriggerLabel ?? "unknown"}";
        var key = (trace.MachineId, transitionKey, trace.Status);
        var count = (_counts.TryGetValue(key, out var current) ? current : 0) + 1;
        _counts[key] = count;
        if (count != _threshold)
            return null;

        var observation = new PromptRouteRepeatBlockObservation(
            trace.MachineId,
            transitionKey,
            trace.Status,
            count,
            $"prompt_route.{trace.Status}.{trace.MachineId}.{transitionKey}");

        if (_lessonEmitter is not null)
            await _lessonEmitter.EmitRepeatBlockLessonAsync(observation, cancellationToken).ConfigureAwait(false);

        return observation;
    }
}
