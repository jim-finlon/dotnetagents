namespace DotNetAgents.Observability.Failures;

public sealed class AgentFallbackPolicy
{
    private readonly IReadOnlyList<AgentFallbackRule> _rules;

    public AgentFallbackPolicy(IEnumerable<AgentFallbackRule> rules)
    {
        _rules = rules?.ToArray() ?? throw new ArgumentNullException(nameof(rules));
    }

    public static AgentFallbackPolicy CreateDefault()
        => new(
            [
                new(
                    AgentFailureKind.DependencyDegraded,
                    AgentFallbackDisposition.UseAlternateProvider,
                    "Route to the next healthy provider or cached read model when available.",
                    "Check dependency health and compare recent failures before retrying."),
                new(
                    AgentFailureKind.Timeout,
                    AgentFallbackDisposition.Retry,
                    "Retry once with backoff, then degrade or escalate if the timeout repeats.",
                    "Inspect latency traces and dependency saturation."),
                new(
                    AgentFailureKind.PolicyRefusal,
                    AgentFallbackDisposition.EscalateToOperator,
                    "Stop mutation and ask for an explicit policy or credential decision.",
                    "Review authorization, story context, and credential custody before retry."),
                new(
                    AgentFailureKind.OddOutput,
                    AgentFallbackDisposition.OpenSdlcFollowUp,
                    "Capture the odd output and route a follow-up story when it repeats.",
                    "Compare repeated-pattern counts and attach representative evidence.",
                    AgentFailureSeverity.Info)
            ]);

    public AgentFallbackRule Resolve(AgentFailureEvent failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return _rules
            .Where(rule => rule.Kind == failure.Kind && failure.Severity >= rule.MinimumSeverity)
            .OrderByDescending(rule => rule.MinimumSeverity)
            .FirstOrDefault()
            ?? new AgentFallbackRule(
                AgentFailureKind.Unknown,
                AgentFallbackDisposition.None,
                "Record telemetry and continue normal error handling.",
                "No failure-type-specific fallback rule matched.",
                AgentFailureSeverity.Info);
    }
}
