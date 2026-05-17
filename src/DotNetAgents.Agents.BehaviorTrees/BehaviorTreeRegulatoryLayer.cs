namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// Activation state returned by an optional regulatory layer before a behavior tree
/// or node is expressed.
/// </summary>
public enum BehaviorTreeRegulatoryActivationState
{
    Unmodified = 0,
    Activated = 1,
    Weighted = 2,
    Suppressed = 3,
    ConditionUnmet = 4
}

/// <summary>One regulatory factor's observable effect on a behavior-tree decision.</summary>
public sealed record BehaviorTreeRegulatoryFactorImpact(
    string FactorId,
    string Kind,
    string Provenance,
    double WeightContribution,
    bool BlockedExpression,
    string Reason);

/// <summary>Regulatory trace for one behavior-tree target.</summary>
public sealed record BehaviorTreeRegulatoryDecisionTrace(
    string TargetNodeId,
    double BaseWeight,
    double EffectiveWeight,
    BehaviorTreeRegulatoryActivationState State,
    IReadOnlyList<BehaviorTreeRegulatoryFactorImpact> Impacts)
{
    public bool BlocksExpression =>
        State is BehaviorTreeRegulatoryActivationState.Suppressed or BehaviorTreeRegulatoryActivationState.ConditionUnmet
        || EffectiveWeight <= 0d;
}

/// <summary>
/// Optional behavior-tree expression hook. Concrete regulatory engines live in
/// higher layers so the behavior-tree package does not depend on evolutionary code.
/// </summary>
public interface IBehaviorTreeRegulatoryLayer<TContext> where TContext : class
{
    ValueTask<BehaviorTreeRegulatoryDecisionTrace> EvaluateAsync(
        string targetNodeId,
        double baseWeight,
        TContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>Optional context-side sink for traces emitted by the executor.</summary>
public interface IBehaviorTreeRegulatoryTraceSink
{
    void RecordRegulatoryTrace(BehaviorTreeRegulatoryDecisionTrace trace);
}
