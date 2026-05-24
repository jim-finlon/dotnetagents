// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace DotNetAgents.Gateway;

// Story 06d1ff68 (foundation slice of 0bb76944). Shared posture contract that
// downstream agents publish and JARVIS routing reads. Pure-additive — the
// contract lives in DotNetAgents.Gateway because it's a cross-service
// substrate (every routing-aware caller will need this); the workflow orchestrator
// posture provider (sibling c28c8183) and JARVIS routing-policy decorator
// (sibling e7576c9c) compile against the records here.
//
// Distinct from health-check or readiness probes: posture summarizes the
// service's *operational stance* — am I actively executing work, waiting on
// a human, recovering from failure, blocked, cooling down — so the routing
// policy can shift load away from a service that's ill-suited for new work,
// not just one that's literally down.

/// <summary>
/// Coarse severity classification of a <see cref="ServicePosture"/>. Routing
/// policy reads this first to decide whether to even consider candidates from
/// a posture-publishing service; finer rationale lives in <see cref="ServicePosture.Reason"/>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ServicePostureSeverity
{
    /// <summary>Service is healthy and accepting new work normally.</summary>
    Healthy = 0,

    /// <summary>Service is operating but degraded — capacity reduced, latency elevated, or fallback paths active. Routing should de-prioritize but may still send work.</summary>
    Degraded = 1,

    /// <summary>Service is paused on an explicit operator gate (e.g. HumanApproval). Routing should not send new work.</summary>
    WaitingForApproval = 2,

    /// <summary>Service is intentionally cooling down after failure or budget exhaustion; not blocked permanently but should not receive load right now.</summary>
    CoolingDown = 3,

    /// <summary>Service is blocked or terminally failed. Routing must not send work.</summary>
    Blocked = 4,
}

/// <summary>
/// Operational stance one DNA service publishes for the routing policy to
/// read. Scoped per-service (a service may post several postures at once if
/// it manages multiple control-loops or surfaces — each gets its own
/// <see cref="Surface"/> tag).
/// </summary>
/// <param name="ServiceId">Stable service identifier — "sdlc-agent",
/// "jarvis", "credentials", etc. Routing keys posture maps off this.</param>
/// <param name="Surface">Optional sub-surface tag when the service exposes
/// multiple control-loops or roles (e.g. "autonomous-loop",
/// "review-queue"). Empty string when the service has one posture for the
/// whole process.</param>
/// <param name="Severity">Coarse classification — first signal the routing
/// policy reads.</param>
/// <param name="Reason">Short human-readable rationale safe for routing
/// receipts and operator dashboards. No secret material.</param>
/// <param name="ObservedAtUtc">When the posture was last refreshed.
/// Routing may treat stale postures (older than a threshold) as Healthy by
/// default to avoid sticky pessimism.</param>
/// <param name="Tags">Optional structured tags for finer routing rules —
/// "retry-pressure", "wall-clock-cap-near", "approval-pending", etc. Keep
/// values short and stable; the routing policy may key dispatch decisions
/// off specific tags.</param>
public sealed record ServicePosture(
    string ServiceId,
    string Surface,
    ServicePostureSeverity Severity,
    string Reason,
    DateTimeOffset ObservedAtUtc,
    IReadOnlyList<string> Tags)
{
    /// <summary>Convenience: whether routing should treat this posture as actively rejecting new work.</summary>
    public bool BlocksRouting =>
        Severity is ServicePostureSeverity.WaitingForApproval
                  or ServicePostureSeverity.CoolingDown
                  or ServicePostureSeverity.Blocked;
}

/// <summary>
/// Service-side contract every posture-publishing component implements.
/// JARVIS routing policy resolves all registered providers and asks each
/// for its current posture before scoring candidate routes. Implementations
/// MUST return quickly (sub-millisecond) — posture is a hot-path read; the
/// underlying snapshot store, not the network or DB, is the source.
/// </summary>
public interface IServicePostureProvider
{
    /// <summary>
    /// Returns the current posture for this provider's service. Returning
    /// null is allowed and means "I have nothing to publish right now,
    /// treat as Healthy". Routing must tolerate nulls without throwing.
    /// </summary>
    ServicePosture? GetCurrentPosture();
}

/// <summary>
/// Default provider that publishes nothing — used to keep the routing
/// dependency-injection graph happy when a service hasn't wired its real
/// provider yet. Returns null on every call so routing falls back to its
/// non-posture-aware behavior.
/// </summary>
public sealed class NullServicePostureProvider : IServicePostureProvider
{
    public ServicePosture? GetCurrentPosture() => null;
}
