namespace DotNetAgents.LaneOps;

/// <summary>
/// Inputs to the runner-class routing policy. Story b6909293.
/// </summary>
public sealed record RunnerRoutingRequest(
    string WorkloadClass,
    string AutonomyTier,
    bool IsHighBlastRadius = false,
    bool OperatorAllowsLocal = false,
    RunnerClass PreferredRunnerClass = RunnerClass.Unspecified,
    bool K3sPodEligible = false,
    bool RequiresPrivilegedSyscalls = false,
    bool RequiresKernelModules = false);

/// <summary>
/// Decision record returned by the routing policy. Story b6909293.
/// </summary>
public sealed record RunnerRoutingDecision(
    bool Admitted,
    RunnerClass RunnerClass,
    string Reason,
    string PolicyVersion);

/// <summary>
/// Pure rule mapping a work-order's workload class + autonomy tier + risk profile to a
/// runner class. Story b6909293 + AUTONOMOUS-AGENT-OPERATING-MODEL-PLAN.md §4.
/// </summary>
/// <remarks>
/// Rules:
/// 1. <c>browser_automation</c> → <see cref="RunnerClass.BrowserRunner"/>.
/// 2. <c>remote_privileged</c> or <c>security_work</c> → <see cref="RunnerClass.PrivilegedLab"/>.
///    Refused on <see cref="RunnerClass.LocalLightweight"/> / <see cref="RunnerClass.CodingVm"/>
///    even if the caller preferred them.
/// 3. <c>docs_spec</c> or <c>contract_spec</c> → <see cref="RunnerClass.DocsSpecRunner"/>.
/// 4. <c>integration_change</c>, <c>repo_refactor</c>, <c>tooling_experiment</c> → <see cref="RunnerClass.CodingVm"/>,
///    unless the caller marks the workload pod-eligible and free of privileged/kernel needs, in which case it
///    may use <see cref="RunnerClass.K3sCodingWorker"/>.
/// 5. <see cref="AutonomyTiers.SupervisedLocal"/> tier with operator-allows-local may use
///    <see cref="RunnerClass.LocalLightweight"/> for non-privileged classes.
/// 6. High-blast-radius work refuses to <see cref="RunnerClass.LocalLightweight"/> regardless
///    of operator opt-in.
/// 7. Unknown workload class → refuse with structured reason; never silent fallback.
/// </remarks>
public static class RunnerRoutingPolicy
{
    /// <summary>Stable policy version. Bump when adding rules; lease records pin this for audit replay.</summary>
    public const string Version = "lane-ops.runner-routing.v1";

    public static RunnerRoutingDecision Route(RunnerRoutingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.WorkloadClass))
            return Refused("workloadClass is required.", request);
        if (string.IsNullOrWhiteSpace(request.AutonomyTier))
            return Refused("autonomyTier is required.", request);

        var workload = request.WorkloadClass.Trim().ToLowerInvariant();
        var tier = request.AutonomyTier.Trim().ToLowerInvariant();

        // Privileged work is always PrivilegedLab.
        if (workload is WorkloadClasses.RemotePrivileged or WorkloadClasses.SecurityWork)
        {
            if (request.PreferredRunnerClass is RunnerClass.LocalLightweight or RunnerClass.CodingVm)
                return Refused(
                    $"RouteRefused: workloadClass='{workload}' requires PrivilegedLab; preferredRunnerClass='{request.PreferredRunnerClass}' is unsafe.",
                    request);
            return Admitted(RunnerClass.PrivilegedLab, $"workloadClass={workload} → PrivilegedLab.");
        }

        if (workload == WorkloadClasses.BrowserAutomation)
            return Admitted(RunnerClass.BrowserRunner, $"workloadClass={workload} → BrowserRunner.");

        if (workload is WorkloadClasses.DocsSpec or WorkloadClasses.ContractSpec)
            return Admitted(RunnerClass.DocsSpecRunner, $"workloadClass={workload} → DocsSpecRunner.");

        if (workload is WorkloadClasses.IntegrationChange or WorkloadClasses.RepoRefactor or WorkloadClasses.ToolingExperiment)
        {
            // SupervisedLocal tier with explicit operator opt-in may use LocalLightweight,
            // but never for high-blast work.
            if (tier == AutonomyTiers.SupervisedLocal && request.OperatorAllowsLocal && !request.IsHighBlastRadius)
                return Admitted(RunnerClass.LocalLightweight,
                    $"workloadClass={workload}, tier={tier}, operatorAllowsLocal=true → LocalLightweight.");

            if (request.IsHighBlastRadius && request.PreferredRunnerClass == RunnerClass.LocalLightweight)
                return Refused(
                    "RouteRefused: high-blast-radius work cannot run on LocalLightweight even with operator opt-in.",
                    request);

            if (request.K3sPodEligible
                && !request.IsHighBlastRadius
                && !request.RequiresPrivilegedSyscalls
                && !request.RequiresKernelModules)
            {
                return Admitted(RunnerClass.K3sCodingWorker,
                    $"workloadClass={workload}, tier={tier}, k3sPodEligible=true → K3sCodingWorker.");
            }

            return Admitted(RunnerClass.CodingVm, $"workloadClass={workload}, tier={tier} → CodingVm.");
        }

        return Refused($"RouteRefused: workloadClass='{workload}' is not in the closed runner-routing vocabulary.", request);
    }

    private static RunnerRoutingDecision Admitted(RunnerClass runnerClass, string reason) =>
        new(true, runnerClass, reason, Version);

    private static RunnerRoutingDecision Refused(string reason, RunnerRoutingRequest request) =>
        new(false, RunnerClass.Unspecified, reason, Version);
}
