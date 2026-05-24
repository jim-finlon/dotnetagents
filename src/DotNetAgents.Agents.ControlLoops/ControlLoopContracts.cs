// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Agents.ControlLoops;

// Story 121a8afc. Canonical control-loop composition contracts. Service-agnostic
// — SDLC, security, infrastructure, evolutionary services all describe their
// loops with the same shapes. This package COMPOSES the existing Workflow /
// StateMachines / BehaviorTrees / Tasks primitives; it never replaces them.
//
// Naming convention: every record describes a *snapshot at a moment in time*.
// Long-running state lives in the underlying primitive (workflow run, state
// machine instance, task queue). These records are what the operator UI +
// telemetry pipeline + governance hooks read.

/// <summary>
/// Identity + correlation metadata for a single control-loop iteration.
/// Threaded through every snapshot a loop emits so observability can group
/// events by run + actor + story without each contract repeating the fields.
/// </summary>
public sealed record ControlLoopRunContext(
    string ControlLoopKey,
    Guid RunId,
    Guid? CorrelationId,
    string? ActorId,
    string? ActorType,
    string? StoryId,
    DateTime StartedAtUtc)
{
    /// <summary>Convenience: a fresh run context for the given key with a generated RunId stamped now.</summary>
    public static ControlLoopRunContext NewRun(string controlLoopKey, string? actorId = null, string? actorType = null, Guid? correlationId = null, string? storyId = null) =>
        new(controlLoopKey, Guid.NewGuid(), correlationId, actorId, actorType, storyId, DateTime.UtcNow);
}

/// <summary>
/// Snapshot of where a service is in its lifecycle. Captures both the formal
/// state (the underlying state machine name) and the operator-readable phase
/// (e.g. "WaitingForApproval", "RetryingAfterTransient", "Steady"). Phase is
/// free-form because each service exposes its own set; State must come from a
/// stable enum the service publishes.
/// </summary>
public sealed record LifecycleStateSnapshot(
    string State,
    string Phase,
    DateTime EnteredAtUtc,
    string? Reason = null,
    bool IsTerminal = false);

/// <summary>
/// One thing the operator should look at. Used by the operator UI to surface
/// stuck items, blocked decisions, expiring approvals, etc. Severity is the
/// shared P0..P3 bucket — same scale used by SDLC stories so dashboards can
/// merge attention items across services.
/// </summary>
public sealed record AttentionItem(
    string Code,
    string Title,
    string Severity,
    DateTime DetectedAtUtc,
    string? Detail = null,
    EvidenceReference? Evidence = null);

/// <summary>
/// Pointer to evidence for a decision the loop made — a test run, an LLM
/// transcript, a tool call result, a policy gate decision, etc. The recorder
/// produces these and the operator UI / governance audit consume them.
///
/// Kind is free-form so services can introduce new evidence kinds without
/// touching this package. Examples: "test.run", "llm.transcript",
/// "tool.call", "policy.decision", "decision-graph.run".
/// </summary>
public sealed record EvidenceReference(
    string Kind,
    string Id,
    string? Source = null,
    string? Url = null,
    DateTime? CapturedAtUtc = null);

/// <summary>
/// Where the loop's queues + dispatchers stand right now. Counts are
/// per-bucket; <see cref="OldestPendingAgeSeconds"/> exposes the latency
/// signal that matters most for ops dashboards.
/// </summary>
public sealed record QueueDispatchSummary(
    string QueueKey,
    int Pending,
    int InFlight,
    int CompletedLastMinute,
    int FailedLastMinute,
    int? OldestPendingAgeSeconds = null,
    string? DispatcherStatus = null,
    int Retrying = 0,
    int? MaxDepthBudget = null,
    string? Backpressure = null);

/// <summary>
/// "Why are we allowed to do what we are about to do?" Records the policy
/// posture the loop is currently operating under — confirmation gates, blast
/// radius, secret access, deploy-window status. Governance hooks (audit,
/// approval, rollback) read this snapshot before acting.
/// </summary>
public sealed record GovernancePosture(
    bool RequiresOperatorConfirmation,
    string BlastRadius,
    string DataClassification,
    bool DeployWindowOpen,
    bool SecretAccessAllowed,
    IReadOnlyList<string>? ActiveGates = null,
    string? PolicyVersion = null);

/// <summary>
/// Top-level health summary. The operator UI's per-service tile reads this.
/// Status is one of: Healthy, Degraded, Stuck, Stopped — those four words
/// are the canonical vocabulary every service in the platform must use.
/// </summary>
public sealed record ControlLoopHealthSummary(
    string Status,
    string Headline,
    DateTime ObservedAtUtc,
    LifecycleStateSnapshot Lifecycle,
    GovernancePosture Governance,
    IReadOnlyList<AttentionItem> Attention,
    IReadOnlyList<QueueDispatchSummary> Queues,
    IReadOnlyList<EvidenceReference> RecentEvidence);

/// <summary>
/// The full publishable shape of a control loop's current run. Services emit
/// one of these (typically on a heartbeat) so the operator UI + telemetry
/// pipeline + governance audit see a consistent picture without each
/// integration querying separate endpoints.
/// </summary>
public sealed record ControlLoopRunSnapshot(
    ControlLoopRunContext RunContext,
    ControlLoopHealthSummary Health,
    IReadOnlyDictionary<string, string>? FreeFormTags = null);
