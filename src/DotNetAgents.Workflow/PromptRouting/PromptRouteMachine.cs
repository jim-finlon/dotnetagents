namespace DotNetAgents.Workflow.PromptRouting;

/// <summary>
/// Safety gate kind attached to a transition. The runner refuses a transition when its
/// gate is not satisfied; the operator can pass a context with the gate condition
/// declared satisfied to proceed.
/// </summary>
public enum PromptRouteSafetyGate
{
    NoGate = 0,
    /// Transition requires an operator-confirm token in the context.
    RequiresOperatorConfirm = 1,
    /// Transition is refused outright when an "unsafe intent" predicate fires in the context.
    BlocksOnUnsafeIntent = 2
}

/// <summary>
/// One state in the prompt-route machine. <see cref="EntryPromptId"/> and
/// <see cref="ExitPromptId"/> are PromptSpecialist catalog references resolved by the
/// live-loop adapter at runtime. <see cref="EntryPrompt"/> and <see cref="ExitPrompt"/>
/// remain optional seed fallbacks for local samples and tests.
/// <see cref="IsResetSink"/> marks the state as a valid landing pad for an intent reset —
/// usually the initial state or a dedicated idle state.
/// </summary>
public sealed record PromptRouteState(
    string StateId,
    string EntryPromptId,
    string ExitPromptId,
    bool IsResetSink,
    string? EntryPrompt = null,
    string? ExitPrompt = null);

public sealed record PromptRouteTransition(
    string FromStateId,
    string ToStateId,
    string TriggerLabel,
    string? GuardPredicateId,
    PromptRouteSafetyGate SafetyGate);

public sealed record PromptRouteMachine(
    string MachineId,
    string InitialStateId,
    IReadOnlyList<PromptRouteState> States,
    IReadOnlyList<PromptRouteTransition> Transitions);

public sealed record PromptRouteContext(
    IReadOnlySet<string> SatisfiedGuardPredicateIds,
    bool OperatorConfirmedTransition,
    bool UnsafeIntentDetected);

public enum PromptRouteTransitionStatus
{
    Transitioned = 0,
    Reset = 1,
    UnknownTrigger = 2,
    GuardUnsatisfied = 3,
    SafetyGateRefused = 4,
    UnsafeIntentBlocked = 5,
    UnknownState = 6
}

public sealed record PromptRouteStep(
    PromptRouteTransitionStatus Status,
    string FromStateId,
    string? ToStateId,
    string? SelectedPrompt,
    string? ExitPrompt,
    string Reason,
    IReadOnlyList<string> EvidenceRefs,
    string? SelectedPromptId = null,
    string? ExitPromptId = null);

/// <summary>
/// Pure prompt-route runner. Story dde7ece6: agents shift intent through a state-
/// machine and reset cleanly so contradictory prompt context doesn't accumulate.
/// The runner is deterministic and side-effect-free; callers project their live
/// agent state onto <see cref="PromptRouteContext"/> before each step.
/// </summary>
public static class PromptRouteRunner
{
    public static PromptRouteStep Transition(
        PromptRouteMachine machine,
        string currentStateId,
        string triggerLabel,
        PromptRouteContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentStateId);
        ArgumentException.ThrowIfNullOrWhiteSpace(triggerLabel);
        context ??= new PromptRouteContext(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            OperatorConfirmedTransition: false,
            UnsafeIntentDetected: false);

        var fromState = machine.States.FirstOrDefault(s =>
            string.Equals(s.StateId, currentStateId, StringComparison.OrdinalIgnoreCase));
        if (fromState is null)
        {
            return new PromptRouteStep(
                PromptRouteTransitionStatus.UnknownState,
                FromStateId: currentStateId,
                ToStateId: null,
                SelectedPrompt: null,
                ExitPrompt: null,
                Reason: $"State '{currentStateId}' is not part of machine '{machine.MachineId}'.",
                EvidenceRefs: new[] { $"machine:{machine.MachineId}", $"unknown-state:{currentStateId}" });
        }

        var transition = machine.Transitions.FirstOrDefault(t =>
            string.Equals(t.FromStateId, currentStateId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(t.TriggerLabel, triggerLabel, StringComparison.OrdinalIgnoreCase));
        if (transition is null)
        {
            return new PromptRouteStep(
                PromptRouteTransitionStatus.UnknownTrigger,
                FromStateId: currentStateId,
                ToStateId: null,
                SelectedPrompt: null,
                ExitPrompt: null,
                Reason: $"No transition '{triggerLabel}' from state '{currentStateId}'.",
                EvidenceRefs: new[] { $"machine:{machine.MachineId}", $"trigger:{triggerLabel}", $"from:{currentStateId}" });
        }

        if (!GuardSatisfied(transition, context))
        {
            return new PromptRouteStep(
                PromptRouteTransitionStatus.GuardUnsatisfied,
                FromStateId: currentStateId,
                ToStateId: null,
                SelectedPrompt: null,
                ExitPrompt: null,
                Reason: $"Guard predicate '{transition.GuardPredicateId}' not satisfied for transition '{triggerLabel}'.",
                EvidenceRefs: new[]
                {
                    $"machine:{machine.MachineId}",
                    $"transition:{currentStateId}->{transition.ToStateId}",
                    $"guard:{transition.GuardPredicateId}"
                });
        }

        switch (transition.SafetyGate)
        {
            case PromptRouteSafetyGate.RequiresOperatorConfirm when !context.OperatorConfirmedTransition:
                return new PromptRouteStep(
                    PromptRouteTransitionStatus.SafetyGateRefused,
                    FromStateId: currentStateId,
                    ToStateId: null,
                    SelectedPrompt: null,
                    ExitPrompt: null,
                    Reason: $"Transition '{triggerLabel}' requires operator confirmation; none supplied.",
                    EvidenceRefs: new[]
                    {
                        $"machine:{machine.MachineId}",
                        $"transition:{currentStateId}->{transition.ToStateId}",
                        "safety-gate:requires-operator-confirm"
                    });
            case PromptRouteSafetyGate.BlocksOnUnsafeIntent when context.UnsafeIntentDetected:
                return new PromptRouteStep(
                    PromptRouteTransitionStatus.UnsafeIntentBlocked,
                    FromStateId: currentStateId,
                    ToStateId: null,
                    SelectedPrompt: null,
                    ExitPrompt: null,
                    Reason: $"Transition '{triggerLabel}' refused: unsafe intent detected in context.",
                    EvidenceRefs: new[]
                    {
                        $"machine:{machine.MachineId}",
                        $"transition:{currentStateId}->{transition.ToStateId}",
                        "safety-gate:blocks-on-unsafe-intent"
                    });
        }

        var toState = machine.States.First(s =>
            string.Equals(s.StateId, transition.ToStateId, StringComparison.OrdinalIgnoreCase));
        return new PromptRouteStep(
            PromptRouteTransitionStatus.Transitioned,
            FromStateId: currentStateId,
            ToStateId: toState.StateId,
            SelectedPrompt: toState.EntryPrompt ?? toState.EntryPromptId,
            ExitPrompt: fromState.ExitPrompt ?? fromState.ExitPromptId,
            Reason: $"Transitioned '{currentStateId}' → '{toState.StateId}' on '{triggerLabel}'.",
            EvidenceRefs: new[]
            {
                $"machine:{machine.MachineId}",
                $"transition:{currentStateId}->{toState.StateId}",
                $"trigger:{triggerLabel}"
            },
            SelectedPromptId: toState.EntryPromptId,
            ExitPromptId: fromState.ExitPromptId);
    }

    /// <summary>
    /// Clean reset: returns the runner to the initial state (or the nearest reset sink
    /// if the initial state isn't itself a reset sink) and clears accumulated context
    /// by emitting the current state's exit prompt + initial state's entry prompt.
    /// </summary>
    public static PromptRouteStep Reset(PromptRouteMachine machine, string currentStateId)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentStateId);

        var fromState = machine.States.FirstOrDefault(s =>
            string.Equals(s.StateId, currentStateId, StringComparison.OrdinalIgnoreCase));
        var initial = machine.States.First(s =>
            string.Equals(s.StateId, machine.InitialStateId, StringComparison.OrdinalIgnoreCase));
        var sinkState = initial.IsResetSink
            ? initial
            : machine.States.FirstOrDefault(s => s.IsResetSink) ?? initial;

        return new PromptRouteStep(
            PromptRouteTransitionStatus.Reset,
            FromStateId: currentStateId,
            ToStateId: sinkState.StateId,
            SelectedPrompt: sinkState.EntryPrompt ?? sinkState.EntryPromptId,
            ExitPrompt: fromState?.ExitPrompt ?? fromState?.ExitPromptId,
            Reason: $"Reset to '{sinkState.StateId}' from '{currentStateId}'. Accumulated intent context cleared.",
            EvidenceRefs: new[]
            {
                $"machine:{machine.MachineId}",
                $"reset:{currentStateId}->{sinkState.StateId}"
            },
            SelectedPromptId: sinkState.EntryPromptId,
            ExitPromptId: fromState?.ExitPromptId);
    }

    private static bool GuardSatisfied(PromptRouteTransition transition, PromptRouteContext context) =>
        transition.GuardPredicateId is null ||
        context.SatisfiedGuardPredicateIds.Contains(transition.GuardPredicateId);
}

/// <summary>
/// Sample fixture: a DNA-style agent flow that shifts intent between Sdlc → CodeReview →
/// Documentation states with an explicit Reset sink. Demonstrates AC#3 (intent shift +
/// clean reset).
/// </summary>
public static class DnaPromptRouteSamples
{
    public const string SdlcStateId = "sdlc";
    public const string CodeReviewStateId = "code-review";
    public const string DocsStateId = "docs";
    public const string IdleStateId = "idle";

    public static PromptRouteMachine DnaAgentRoute { get; } = new(
        MachineId: "dna.agent-intent",
        InitialStateId: IdleStateId,
        States: new[]
        {
            new PromptRouteState(
                StateId: IdleStateId,
                EntryPromptId: "jarvis.intent-shift.idle.entry.v1",
                ExitPromptId: "jarvis.intent-shift.idle.exit.v1",
                IsResetSink: true,
                EntryPrompt: "You are an idle DNA assistant. Wait for the operator's next intent.",
                ExitPrompt: "Park idle context before shifting intent."),
            new PromptRouteState(
                StateId: SdlcStateId,
                EntryPromptId: "sdlc.claim.entry.v1",
                ExitPromptId: "sdlc.claim.exit.v1",
                IsResetSink: false,
                EntryPrompt: "You are now operating in the SDLC story-claim intent. Surface lane-ready blockers first.",
                ExitPrompt: "Park SDLC context: any in-progress claim attempt is paused."),
            new PromptRouteState(
                StateId: CodeReviewStateId,
                EntryPromptId: "code-review.security.entry.v1",
                ExitPromptId: "code-review.security.exit.v1",
                IsResetSink: false,
                EntryPrompt: "You are now operating in the code-review intent. Prioritize security-identity findings on infrastructure-mutating diffs.",
                ExitPrompt: "Park code-review context: any open review thread is summarized but not closed."),
            new PromptRouteState(
                StateId: DocsStateId,
                EntryPromptId: "docs.intent.entry.v1",
                ExitPromptId: "docs.intent.exit.v1",
                IsResetSink: false,
                EntryPrompt: "You are now operating in the documentation intent. Lead with an intent statement before the diff.",
                ExitPrompt: "Park docs context: any draft is saved.")
        },
        Transitions: new[]
        {
            new PromptRouteTransition(IdleStateId, SdlcStateId, "intent.shift-to-sdlc", GuardPredicateId: null, PromptRouteSafetyGate.NoGate),
            new PromptRouteTransition(IdleStateId, CodeReviewStateId, "intent.shift-to-code-review", GuardPredicateId: null, PromptRouteSafetyGate.NoGate),
            new PromptRouteTransition(IdleStateId, DocsStateId, "intent.shift-to-docs", GuardPredicateId: null, PromptRouteSafetyGate.NoGate),
            new PromptRouteTransition(SdlcStateId, CodeReviewStateId, "intent.shift-to-code-review", GuardPredicateId: "story.has-open-pr", PromptRouteSafetyGate.NoGate),
            new PromptRouteTransition(CodeReviewStateId, DocsStateId, "intent.shift-to-docs", GuardPredicateId: null, PromptRouteSafetyGate.NoGate),
            new PromptRouteTransition(SdlcStateId, DocsStateId, "intent.shift-to-docs-direct", GuardPredicateId: null, PromptRouteSafetyGate.RequiresOperatorConfirm),
            new PromptRouteTransition(DocsStateId, SdlcStateId, "intent.shift-to-sdlc", GuardPredicateId: null, PromptRouteSafetyGate.BlocksOnUnsafeIntent)
        });
}
