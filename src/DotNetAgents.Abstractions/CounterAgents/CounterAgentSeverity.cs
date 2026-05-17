namespace DotNetAgents.Abstractions.CounterAgents;

/// <summary>
/// Severity bucket for a counter-agent verdict. Combined with <see cref="CounterAgentVerdictKind"/>
/// to express both the *what* (approve/concern/block) and the *how strongly* (severity).
/// </summary>
/// <remarks>
/// Aggregator policy uses severity to rank across multiple counter-agents — the highest-severity
/// non-Approve verdict wins. <see cref="Trivial"/> concerns are typically logged but not surfaced
/// to the operator dashboard; <see cref="Critical"/> concerns mandate operator attention even if
/// the verdict is <see cref="CounterAgentVerdictKind.Concern"/> (not Block).
/// </remarks>
public enum CounterAgentSeverity
{
    /// <summary>Trivial — recorded but typically not surfaced to operators.</summary>
    Trivial = 0,

    /// <summary>Minor — logged; surfaced in dashboards but not dashboard alerts.</summary>
    Minor = 1,

    /// <summary>Moderate — surfaced in dashboard with a notification.</summary>
    Moderate = 2,

    /// <summary>Major — operator alert; non-blocking concerns of this severity still mandate attention.</summary>
    Major = 3,

    /// <summary>Critical — typical default for <see cref="CounterAgentVerdictKind.Block"/>; always surfaced as alert.</summary>
    Critical = 4,
}
