// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.ReleasePolicy;

/// <summary>
/// Authoritative decision the policy FSM reaches for a single deploy run.
/// Codified per <c>docs/sdlc-governance/RELEASE-POLICY-FSM.md</c>.
/// </summary>
public enum ReleasePolicyDecision
{
    /// Roll back the deploy via the existing rollback path.
    Rollback = 0,
    /// Surface a warning in cockpit; do not roll back, do not auto-file a bug.
    WarnAndContinue = 1,
    /// Continue the rollout but auto-create a Bug-typed SDLC story.
    ContinueAndFileBug = 2,
    /// Stop the auto-rollout and emit a review decision record form for operator review.
    HaltForOperator = 3
}

public enum ReleaseBlastRadius
{
    LabOnly = 0,
    NonCore4 = 1,
    Core4 = 2
}

public enum DeployRunGateOutcome
{
    /// Smoke or deploy gate passed.
    Passed = 0,
    /// Smoke or deploy gate failed.
    Failed = 1
}

/// <summary>
/// Inputs the evaluator needs about the deploy run itself.
/// </summary>
public sealed record DeployRunSummary(
    string DeployRunId,
    string ServiceName,
    string FailedGate,
    string FailureClass,
    DeployRunGateOutcome Outcome,
    DateTime CompletedAtUtc);

/// <summary>
/// Inputs that locate the deploy run in the release-policy decision space:
/// blast radius, how long since the last green deploy, and whether this same
/// failure class fired within the recent window.
/// </summary>
public sealed record ImpactAssessment(
    ReleaseBlastRadius BlastRadius,
    TimeSpan? TimeSinceLastSuccessfulDeploy,
    bool SameFailureClassWithinRecentWindow,
    string? LastSuccessfulDeployRunId);

/// <summary>
/// Verdict returned by <see cref="IReleasePolicyEvaluator"/>. The evaluator never
/// performs the rollback itself — it emits an intent + rationale + evidence refs;
/// the infrastructure-control identity is what actually rolls back.
/// </summary>
public sealed record ReleasePolicyVerdict(
    ReleasePolicyDecision Decision,
    string Rationale,
    IReadOnlyList<string> EvidenceRefs);

public interface IReleasePolicyEvaluator
{
    ReleasePolicyVerdict Evaluate(DeployRunSummary deployRun, ImpactAssessment impact);
}

public sealed record ReleasePolicyEvaluatorOptions
{
    /// Same-failure-class escalation window for Core4 deploys.
    public TimeSpan Core4SameFailureClassWindow { get; init; } = TimeSpan.FromHours(1);
    /// Boundary between "recent" and "stale" last-successful-deploy on non-Core4.
    public TimeSpan NonCore4StaleSuccessThreshold { get; init; } = TimeSpan.FromHours(24);
    /// Operator can supply an explicit override at deploy-run time; the evaluator
    /// emits that decision with the supplied rationale and a `policy:override` evidence ref.
    public ReleasePolicyDecision? OperatorOverride { get; init; }
    public string? OperatorOverrideRationale { get; init; }
    public string? OperatorOverrideActorId { get; init; }
}

/// <summary>
/// Pure evaluator implementing the truth table from <c>RELEASE-POLICY-FSM.md</c>.
/// No I/O, no DI, no <c>DateTime.UtcNow</c> — callers supply the deploy-run
/// completion timestamp + the impact assessment.
/// </summary>
public sealed class DefaultReleasePolicyEvaluator : IReleasePolicyEvaluator
{
    private readonly ReleasePolicyEvaluatorOptions _options;

    public DefaultReleasePolicyEvaluator(ReleasePolicyEvaluatorOptions? options = null)
    {
        _options = options ?? new ReleasePolicyEvaluatorOptions();
    }

    public ReleasePolicyVerdict Evaluate(DeployRunSummary deployRun, ImpactAssessment impact)
    {
        ArgumentNullException.ThrowIfNull(deployRun);
        ArgumentNullException.ThrowIfNull(impact);

        // Operator override is the highest-priority outcome — exposed as a
        // break-glass per RELEASE-POLICY-FSM §Override mechanism.
        if (_options.OperatorOverride is ReleasePolicyDecision overrideDecision)
        {
            var rationale = _options.OperatorOverrideRationale ??
                            "Operator override applied without supplied rationale.";
            return new ReleasePolicyVerdict(
                overrideDecision,
                $"Operator override → {overrideDecision}: {rationale}",
                Evidence(deployRun, impact, extra: $"policy:override:{_options.OperatorOverrideActorId ?? "unknown"}"));
        }

        if (deployRun.Outcome == DeployRunGateOutcome.Passed)
        {
            return new ReleasePolicyVerdict(
                ReleasePolicyDecision.WarnAndContinue,
                "Deploy gate passed; no policy action required.",
                Evidence(deployRun, impact));
        }

        return impact.BlastRadius switch
        {
            ReleaseBlastRadius.Core4 when impact.SameFailureClassWithinRecentWindow =>
                new ReleasePolicyVerdict(
                    ReleasePolicyDecision.HaltForOperator,
                    $"Core4 + same-failure-class within {_options.Core4SameFailureClassWindow}: escalate to review decision record.",
                    Evidence(deployRun, impact, extra: "policy:core4-same-failure")),

            ReleaseBlastRadius.Core4 =>
                new ReleasePolicyVerdict(
                    ReleasePolicyDecision.Rollback,
                    "Core4 first-fail post-deploy: auto-rollback per RELEASE-POLICY-FSM §Core4.",
                    Evidence(deployRun, impact, extra: "policy:core4-first-fail")),

            ReleaseBlastRadius.NonCore4 when StaleNonCore4Success(impact) =>
                new ReleasePolicyVerdict(
                    ReleasePolicyDecision.Rollback,
                    $"Non-Core4 smoke fail with last green > {_options.NonCore4StaleSuccessThreshold}: rollback to last known good.",
                    Evidence(deployRun, impact, extra: "policy:non-core4-stale-success")),

            ReleaseBlastRadius.NonCore4 =>
                new ReleasePolicyVerdict(
                    ReleasePolicyDecision.ContinueAndFileBug,
                    $"Non-Core4 smoke fail with recent green: continue rollout and auto-file a Bug.",
                    Evidence(deployRun, impact, extra: "policy:non-core4-recent-success")),

            ReleaseBlastRadius.LabOnly =>
                new ReleasePolicyVerdict(
                    ReleasePolicyDecision.WarnAndContinue,
                    "Lab-only deploy failure: warn-and-continue plus auto-file a Bug.",
                    Evidence(deployRun, impact, extra: "policy:lab-only-fail")),

            _ => new ReleasePolicyVerdict(
                ReleasePolicyDecision.HaltForOperator,
                "Unrecognized blast-radius value; defaulting to review decision record.",
                Evidence(deployRun, impact, extra: "policy:unknown-blast-radius"))
        };
    }

    private bool StaleNonCore4Success(ImpactAssessment impact) =>
        impact.TimeSinceLastSuccessfulDeploy is TimeSpan since &&
        since > _options.NonCore4StaleSuccessThreshold;

    private static IReadOnlyList<string> Evidence(DeployRunSummary deployRun, ImpactAssessment impact, string? extra = null)
    {
        var refs = new List<string>
        {
            $"deploy-run:{deployRun.DeployRunId}",
            $"service:{deployRun.ServiceName}",
            $"failed-gate:{deployRun.FailedGate}",
            $"failure-class:{deployRun.FailureClass}",
            $"blast-radius:{impact.BlastRadius}"
        };
        if (impact.LastSuccessfulDeployRunId is not null)
            refs.Add($"last-green:{impact.LastSuccessfulDeployRunId}");
        if (impact.TimeSinceLastSuccessfulDeploy is TimeSpan since)
            refs.Add($"since-last-green:{since:c}");
        if (extra is not null)
            refs.Add(extra);
        return refs;
    }
}
