namespace DotNetAgents.LaneOps;

/// <summary>
/// Data-driven runner-routing policy engine. Story 58b726c9: equivalent decision surface to
/// the static <see cref="RunnerRoutingPolicy"/>, but reads its rules from a versioned
/// <see cref="RunnerRoutingBundle"/> so operators can amend the policy without recompiling
/// and so multiple bundle versions can coexist for audit replay.
/// </summary>
public sealed class RunnerRoutingPolicyEngine
{
    private readonly RunnerRoutingBundle _bundle;

    public RunnerRoutingPolicyEngine(RunnerRoutingBundle bundle)
    {
        _bundle = bundle ?? throw new ArgumentNullException(nameof(bundle));
    }

    /// <summary>Bundle this engine evaluates against.</summary>
    public RunnerRoutingBundle Bundle => _bundle;

    /// <summary>
    /// Route a request through the configured rules. Decision shape matches
    /// <see cref="RunnerRoutingPolicy.Route"/> exactly so callers can swap engines per bundle.
    /// </summary>
    public RunnerRoutingDecision Route(RunnerRoutingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.WorkloadClass))
            return Refused("workloadClass is required.");
        if (string.IsNullOrWhiteSpace(request.AutonomyTier))
            return Refused("autonomyTier is required.");

        var workload = request.WorkloadClass.Trim().ToLowerInvariant();
        var tier = request.AutonomyTier.Trim().ToLowerInvariant();

        foreach (var rule in _bundle.Rules)
        {
            if (!RuleMatchesWorkload(rule, workload))
                continue;

            // Step 1: refuse if the caller's preferred runner class is in this rule's forbidden list.
            if (rule.ForbiddenPreferredRunnerClasses is { Count: > 0 } forbidden
                && forbidden.Contains(request.PreferredRunnerClass))
            {
                var template = rule.PreferredRunnerClassRefusalReasonTemplate
                    ?? $"RouteRefused: workloadClass='{{workload}}' refuses preferredRunnerClass='{{preferred}}'.";
                return Refused(RenderTemplate(template, workload, tier, request));
            }

            // Step 2: alternates win over the high-blast-refusal-of-preferred check below.
            if (rule.Alternates is { Count: > 0 } alternates)
            {
                foreach (var alt in alternates)
                {
                    if (AlternateMatches(alt, tier, request))
                        return Admitted(alt.RunnerClass, RenderTemplate(alt.ReasonTemplate, workload, tier, request));
                }
            }

            // Step 3: refuse if high-blast and the caller preferred a runner class flagged unsafe under high-blast.
            if (request.IsHighBlastRadius
                && rule.HighBlastRefusedPreferredRunnerClasses is { Count: > 0 } highBlastRefused
                && highBlastRefused.Contains(request.PreferredRunnerClass))
            {
                var template = rule.HighBlastPreferredRefusalReasonTemplate
                    ?? "RouteRefused: high-blast-radius work cannot run on preferredRunnerClass='{preferred}'.";
                return Refused(RenderTemplate(template, workload, tier, request));
            }

            // Step 4: default to the rule's primary runner class.
            return Admitted(rule.PrimaryRunnerClass, RenderTemplate(rule.PrimaryReasonTemplate, workload, tier, request));
        }

        return Refused(RenderTemplate(_bundle.UnknownWorkloadRefusalReasonTemplate, workload, tier, request));
    }

    private static bool RuleMatchesWorkload(RunnerRoutingRule rule, string workload)
    {
        foreach (var candidate in rule.WorkloadClasses)
        {
            if (string.Equals(candidate, workload, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static bool AlternateMatches(RunnerRoutingAlternate alt, string tier, RunnerRoutingRequest request)
    {
        if (!string.IsNullOrWhiteSpace(alt.RequiresAutonomyTier)
            && !string.Equals(alt.RequiresAutonomyTier, tier, StringComparison.OrdinalIgnoreCase))
            return false;
        if (alt.RequiresOperatorAllowsLocal && !request.OperatorAllowsLocal)
            return false;
        if (alt.RefusedIfHighBlastRadius && request.IsHighBlastRadius)
            return false;
        if (alt.RequiresK3sPodEligible && !request.K3sPodEligible)
            return false;
        if (alt.RefusedIfRequiresPrivilegedSyscalls && request.RequiresPrivilegedSyscalls)
            return false;
        if (alt.RefusedIfRequiresKernelModules && request.RequiresKernelModules)
            return false;
        return true;
    }

    private static string RenderTemplate(string template, string workload, string tier, RunnerRoutingRequest request)
    {
        return template
            .Replace("{workload}", workload, StringComparison.Ordinal)
            .Replace("{tier}", tier, StringComparison.Ordinal)
            .Replace("{preferred}", request.PreferredRunnerClass.ToString(), StringComparison.Ordinal);
    }

    private RunnerRoutingDecision Admitted(RunnerClass runnerClass, string reason) =>
        new(true, runnerClass, reason, _bundle.PolicyVersion);

    private RunnerRoutingDecision Refused(string reason) =>
        new(false, RunnerClass.Unspecified, reason, _bundle.PolicyVersion);
}
