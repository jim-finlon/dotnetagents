// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;

namespace DotNetAgents.Observability.LaneOps;

/// <summary>
/// OpenTelemetry helpers for autonomous-lane operations. Story 49d5a676.
/// </summary>
/// <remarks>
/// Every helper either returns the started <see cref="Activity"/> (so callers can wrap
/// in <c>using</c>) or returns null when no listener is registered (zero-cost path).
///
/// The single shared <c>ActivitySource("DotNetAgents.LaneOps")</c> is registered by
/// <c>OpenTelemetryExtensions.AddDotNetAgentsInstrumentation</c>; downstream services
/// (credential resolver, infrastructure-control-agent) get span propagation by attaching
/// to the same source name + extracting <see cref="LaneTraceTags.WorkOrderId"/> from
/// inbound A2A headers (follow-up story for header propagation).
/// </remarks>
public static class LaneTracingExtensions
{
    /// <summary>Activity source name registered by <c>AddDotNetAgentsInstrumentation</c>.</summary>
    public const string SourceName = "DotNetAgents.LaneOps";

    /// <summary>Shared <see cref="ActivitySource"/> for all lane-lifecycle spans.</summary>
    public static readonly ActivitySource ActivitySource = new(SourceName);

    /// <summary>
    /// Start a span covering one autonomous-lane phase transition (e.g., the lifetime of
    /// the Coding phase, the lifetime of the Cadre phase). Wrap in <c>using</c> so the
    /// span closes on dispose.
    /// </summary>
    /// <param name="workOrderId">Required correlation key.</param>
    /// <param name="phase">Lane lifecycle phase name (typically <c>LanePhase</c> enum string).</param>
    /// <param name="laneId">Optional stable lane id when known.</param>
    /// <param name="runnerClass">Optional runner class label.</param>
    public static Activity? StartLanePhaseActivity(
        Guid workOrderId,
        string phase,
        string? laneId = null,
        string? runnerClass = null)
    {
        var activity = ActivitySource.StartActivity($"lane.phase.{phase.ToLowerInvariant()}");
        if (activity is null)
            return null;

        activity.SetTag(LaneTraceTags.WorkOrderId, workOrderId.ToString());
        activity.SetTag(LaneTraceTags.Phase, phase);
        if (!string.IsNullOrWhiteSpace(laneId))
            activity.SetTag(LaneTraceTags.LaneId, laneId);
        if (!string.IsNullOrWhiteSpace(runnerClass))
            activity.SetTag(LaneTraceTags.RunnerClass, runnerClass);
        return activity;
    }

    /// <summary>
    /// Start a span covering a lease bind/unbind operation on a work order.
    /// </summary>
    public static Activity? StartLaneLeaseActivity(
        string operation,
        Guid workOrderId,
        string leaseKind,
        Guid? leaseId = null,
        string? reason = null)
    {
        var activity = ActivitySource.StartActivity($"lane.lease.{operation.ToLowerInvariant()}");
        if (activity is null)
            return null;

        activity.SetTag(LaneTraceTags.WorkOrderId, workOrderId.ToString());
        activity.SetTag("lane.lease.kind", leaseKind);
        if (leaseId is { } id)
            activity.SetTag(LeaseTagFor(leaseKind), id.ToString());
        if (!string.IsNullOrWhiteSpace(reason))
            activity.SetTag(LaneTraceTags.Reason, reason);
        return activity;
    }

    /// <summary>
    /// Start a span covering a lane cleanup operation (worktree removal, sandbox destroy).
    /// </summary>
    public static Activity? StartLaneCleanupActivity(
        Guid workOrderId,
        string? laneId = null,
        string? cleanupOutcome = null)
    {
        var activity = ActivitySource.StartActivity("lane.cleanup");
        if (activity is null)
            return null;

        activity.SetTag(LaneTraceTags.WorkOrderId, workOrderId.ToString());
        if (!string.IsNullOrWhiteSpace(laneId))
            activity.SetTag(LaneTraceTags.LaneId, laneId);
        if (!string.IsNullOrWhiteSpace(cleanupOutcome))
            activity.SetTag(LaneTraceTags.CleanupOutcome, cleanupOutcome);
        return activity;
    }

    /// <summary>
    /// Start a span covering an acceptance-workflow transition (repair/retry/reclaim/peer-verify).
    /// Cross-links to story 5628f0f2.
    /// </summary>
    public static Activity? StartAcceptanceTransitionActivity(
        Guid workOrderId,
        Guid storyId,
        string transitionKind,
        string? initiatorActorType = null,
        string? initiatorActorId = null,
        string? reason = null)
    {
        var activity = ActivitySource.StartActivity($"lane.transition.{transitionKind.ToLowerInvariant()}");
        if (activity is null)
            return null;

        activity.SetTag(LaneTraceTags.WorkOrderId, workOrderId.ToString());
        activity.SetTag(LaneTraceTags.StoryId, storyId.ToString());
        activity.SetTag(LaneTraceTags.TransitionKind, transitionKind);
        if (!string.IsNullOrWhiteSpace(initiatorActorType))
            activity.SetTag(LaneTraceTags.InitiatorActorType, initiatorActorType);
        if (!string.IsNullOrWhiteSpace(initiatorActorId))
            activity.SetTag(LaneTraceTags.InitiatorActorId, initiatorActorId);
        if (!string.IsNullOrWhiteSpace(reason))
            activity.SetTag(LaneTraceTags.Reason, reason);
        return activity;
    }

    /// <summary>Mark the current lane span as failed with a reason; pairs with a using block.</summary>
    public static void RecordFailure(this Activity? activity, string reason)
    {
        if (activity is null) return;
        activity.SetStatus(ActivityStatusCode.Error, reason);
        activity.SetTag(LaneTraceTags.Reason, reason);
    }

    /// <summary>Mark the current lane span as succeeded with an optional outcome label.</summary>
    public static void RecordSuccess(this Activity? activity, string? outcome = null)
    {
        if (activity is null) return;
        activity.SetStatus(ActivityStatusCode.Ok);
        if (!string.IsNullOrWhiteSpace(outcome))
            activity.SetTag(LaneTraceTags.CleanupOutcome, outcome);
    }

    private static string LeaseTagFor(string leaseKind) => leaseKind.ToLowerInvariant() switch
    {
        "agentinstance" or "agent_instance" => LaneTraceTags.AgentInstanceLeaseId,
        "worktree" => LaneTraceTags.WorktreeLeaseId,
        "sandbox" => LaneTraceTags.SandboxLeaseId,
        "auth" => LaneTraceTags.AuthLeaseId,
        _ => "lane.lease.id",
    };
}
