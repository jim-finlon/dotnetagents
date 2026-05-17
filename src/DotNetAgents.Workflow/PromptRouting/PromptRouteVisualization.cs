namespace DotNetAgents.Workflow.PromptRouting;

public sealed record PromptRouteMachineView(
    string MachineId,
    string CurrentStateId,
    IReadOnlyList<PromptRouteStateView> States,
    IReadOnlyList<PromptRouteTransitionView> Transitions,
    IReadOnlyList<PromptRouteTransitionTrace> Trace);

public sealed record PromptRouteStateView(
    string StateId,
    string EntryPromptId,
    string ExitPromptId,
    bool IsResetSink,
    bool IsCurrent);

public sealed record PromptRouteTransitionView(
    string FromStateId,
    string ToStateId,
    string TriggerLabel,
    string? GuardPredicateId,
    PromptRouteSafetyGate SafetyGate,
    bool IsCurrentEdge);

public static class PromptRouteMachineVisualizationBuilder
{
    public static PromptRouteMachineView Build(
        PromptRouteMachine machine,
        string currentStateId,
        IReadOnlyList<PromptRouteTransitionTrace>? trace = null)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentStateId);

        var traceEntries = trace?.TakeLast(10).ToList() ?? new List<PromptRouteTransitionTrace>();
        var lastTransitioned = traceEntries.LastOrDefault(t => t.Status is PromptRouteTransitionStatus.Transitioned or PromptRouteTransitionStatus.Reset);
        var states = machine.States
            .Select(s => new PromptRouteStateView(
                s.StateId,
                s.EntryPromptId,
                s.ExitPromptId,
                s.IsResetSink,
                string.Equals(s.StateId, currentStateId, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        var transitions = machine.Transitions
            .Select(t => new PromptRouteTransitionView(
                t.FromStateId,
                t.ToStateId,
                t.TriggerLabel,
                t.GuardPredicateId,
                t.SafetyGate,
                lastTransitioned is not null &&
                string.Equals(lastTransitioned.FromStateId, t.FromStateId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(lastTransitioned.ToStateId, t.ToStateId, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return new PromptRouteMachineView(machine.MachineId, currentStateId, states, transitions, traceEntries);
    }
}

public enum PromptRouteMutationLayer
{
    BaseGenome = 0,
    BehaviorTreeLineage = 1,
    RegulatoryLayer = 2
}

public static class PromptRouteLayeredMutationClassifier
{
    public static PromptRouteMutationLayer ClassifyTransitionMutation(PromptRouteTransition _) =>
        PromptRouteMutationLayer.BehaviorTreeLineage;
}

public sealed record PromptRouteRegulatoryWeight(
    string TransitionKey,
    double Weight,
    IReadOnlyList<string> EvidenceRefs);

public static class PromptRouteRegulatoryTransitionSelector
{
    public static PromptRouteTransition? SelectBestTransition(
        IEnumerable<PromptRouteTransition> transitions,
        IReadOnlyList<PromptRouteRegulatoryWeight> weights)
    {
        ArgumentNullException.ThrowIfNull(transitions);
        ArgumentNullException.ThrowIfNull(weights);

        var byKey = weights.ToDictionary(w => w.TransitionKey, StringComparer.OrdinalIgnoreCase);
        return transitions
            .OrderByDescending(t => byKey.TryGetValue(Key(t), out var weight) ? weight.Weight : 1.0)
            .ThenBy(t => t.TriggerLabel, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    public static string Key(PromptRouteTransition transition) =>
        $"{transition.FromStateId}->{transition.ToStateId}:{transition.TriggerLabel}";
}
