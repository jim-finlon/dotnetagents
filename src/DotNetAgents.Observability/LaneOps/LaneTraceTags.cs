namespace DotNetAgents.Observability.LaneOps;

/// <summary>
/// Stable tag-key vocabulary for autonomous-lane OpenTelemetry spans. Story 49d5a676.
/// </summary>
/// <remarks>
/// All tags are namespaced under <c>lane.*</c> so trace consumers can filter, group, and
/// stitch spans across services without colliding with the existing
/// <c>llm.*</c>/<c>workflow.*</c>/<c>agent.*</c>/<c>tool.*</c> conventions in
/// <see cref="DotNetAgents.Observability.Tracing.OpenTelemetryExtensions"/> and
/// <see cref="DotNetAgents.Observability.Agents.AgentTracingExtensions"/>.
///
/// <see cref="WorkOrderId"/> is the trace correlation key — every span emitted on behalf
/// of a single automated worker carries the same WorkOrderId so downstream services
/// (credential resolver, infrastructure-control-agent) can attach to the parent trace via
/// A2A header propagation (follow-up story).
/// </remarks>
public static class LaneTraceTags
{
    /// <summary>Trace correlation key. GUID string of the autonomous work order.</summary>
    public const string WorkOrderId = "lane.work_order_id";

    /// <summary>Lane lifecycle phase enum name (Claimed, Leased, Coding, ...).</summary>
    public const string Phase = "lane.phase";

    /// <summary>Stable lane id (LaneAllocator.LaneId from story 6ba4adb7).</summary>
    public const string LaneId = "lane.id";

    /// <summary>Runner class label (coding_vm, k3s_coding_worker, privileged_lab, docs_only).</summary>
    public const string RunnerClass = "lane.runner_class";

    /// <summary>Story id the work order was claimed for.</summary>
    public const string StoryId = "lane.story_id";

    /// <summary>Agent-instance lease id (UUID).</summary>
    public const string AgentInstanceLeaseId = "lane.lease.agent_instance_id";

    /// <summary>Worktree lease id (UUID).</summary>
    public const string WorktreeLeaseId = "lane.lease.worktree_id";

    /// <summary>Sandbox lease id (UUID) when present.</summary>
    public const string SandboxLeaseId = "lane.lease.sandbox_id";

    /// <summary>Auth/credential lease id (UUID) when present.</summary>
    public const string AuthLeaseId = "lane.lease.auth_id";

    /// <summary>Acceptance-workflow transition kind (Repair, Retry, Reclaim, CounterAgentVerify) — story 5628f0f2 cross-link.</summary>
    public const string TransitionKind = "lane.transition.kind";

    /// <summary>Cleanup outcome label (clean, partial, dirty) emitted on cleanup spans.</summary>
    public const string CleanupOutcome = "lane.cleanup.outcome";

    /// <summary>Reason string for explicit transitions (force-cancel, lease-revoke, retry, reclaim).</summary>
    public const string Reason = "lane.reason";

    /// <summary>Initiator actor type (WorkstationSession, AgentInstance, Human, ...).</summary>
    public const string InitiatorActorType = "lane.initiator.actor_type";

    /// <summary>Initiator actor id (e.g. agent-gamma, agent-alpha).</summary>
    public const string InitiatorActorId = "lane.initiator.actor_id";
}
