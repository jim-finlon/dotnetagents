using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace DotNetAgents.Agents.ControlLoops;

/// <summary>
/// Standard telemetry surface for DNA control loops. Story 35566ab5.
///
/// All control-loop services emit through this single Meter + ActivitySource so
/// dashboards aggregate cross-service without per-service translation glue.
/// Uses System.Diagnostics.Metrics + System.Diagnostics so any OpenTelemetry
/// or Prometheus exporter already configured for the host picks the data up
/// automatically — no separate metrics stack.
///
/// CANONICAL DIMENSIONS (every metric carries these tags unless noted):
///   - control_loop.key        — stable loop identifier (e.g. "sdlc.autonomous-loop")
///   - control_loop.actor_type — Human | AgentInstance | WorkstationSession | ServiceAccount | WorkflowRun | Team
///   - lifecycle.state         — current state from LifecyclePatterns.States vocabulary
///
/// METRIC NAMING (DotNetAgents.ControlLoops meter):
///   - control_loop.tick.total                   counter
///   - control_loop.tick.duration_ms             histogram
///   - control_loop.transition.total             counter (+ from_state, to_state tags)
///   - control_loop.retry.total                  counter (+ attempt tag)
///   - control_loop.escalation.total             counter (+ tier tag)
///   - control_loop.evidence.published.total     counter (+ evidence_kind tag)
///   - control_loop.queue.depth                  observable gauge (instance-set per queue_key)
///   - control_loop.queue.oldest_age_seconds     observable gauge (instance-set per queue_key)
///   - control_loop.attention.total              counter (+ severity, code tags)
///
/// ACTIVITY SOURCE: DotNetAgents.ControlLoops — span name = control_loop.tick / control_loop.transition.
/// </summary>
public static class ControlLoopTelemetry
{
    public const string MeterName = "DotNetAgents.ControlLoops";
    public const string ActivitySourceName = "DotNetAgents.ControlLoops";
    public const string Version = "1.0.0";

    // Standard tag keys — services that emit telemetry should use these constants
    // rather than string literals so the dimension contract stays observable.
    public const string TagControlLoopKey = "control_loop.key";
    public const string TagActorType = "control_loop.actor_type";
    public const string TagLifecycleState = "lifecycle.state";
    public const string TagFromState = "from_state";
    public const string TagToState = "to_state";
    public const string TagAttempt = "attempt";
    public const string TagTier = "tier";
    public const string TagEvidenceKind = "evidence_kind";
    public const string TagQueueKey = "queue_key";
    public const string TagSeverity = "severity";
    public const string TagCode = "code";

    internal static readonly Meter Meter = new(MeterName, Version);
    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, Version);

    internal static readonly Counter<long> TickCounter = Meter.CreateCounter<long>("control_loop.tick.total", description: "Number of control-loop ticks executed.");
    internal static readonly Histogram<double> TickDuration = Meter.CreateHistogram<double>("control_loop.tick.duration_ms", unit: "ms", description: "Control-loop tick duration.");
    internal static readonly Counter<long> TransitionCounter = Meter.CreateCounter<long>("control_loop.transition.total", description: "Lifecycle state transitions.");
    internal static readonly Counter<long> RetryCounter = Meter.CreateCounter<long>("control_loop.retry.total", description: "Retry attempts inside reactive policies.");
    internal static readonly Counter<long> EscalationCounter = Meter.CreateCounter<long>("control_loop.escalation.total", description: "Escalation tier fires.");
    internal static readonly Counter<long> EvidenceCounter = Meter.CreateCounter<long>("control_loop.evidence.published.total", description: "Evidence references published.");
    internal static readonly Counter<long> AttentionCounter = Meter.CreateCounter<long>("control_loop.attention.total", description: "Attention items raised by control loops.");
}

/// <summary>
/// Per-loop recorder that stamps the canonical dimensions on every emission.
/// Story 35566ab5. Service code that emits control-loop telemetry should
/// instantiate one of these (typically once per loop instance) instead of
/// touching the static counters directly — that way the loop key + actor type
/// are guaranteed to be present.
/// </summary>
public sealed class ControlLoopMetricsRecorder
{
    private readonly KeyValuePair<string, object?>[] _baseTags;

    public ControlLoopMetricsRecorder(string controlLoopKey, string actorType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(controlLoopKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorType);
        _baseTags = new[]
        {
            new KeyValuePair<string, object?>(ControlLoopTelemetry.TagControlLoopKey, controlLoopKey),
            new KeyValuePair<string, object?>(ControlLoopTelemetry.TagActorType, actorType),
        };
    }

    public string ControlLoopKey => (string)_baseTags[0].Value!;
    public string ActorType => (string)_baseTags[1].Value!;

    public void Tick(string lifecycleState, double durationMs)
    {
        var tags = WithLifecycle(lifecycleState);
        ControlLoopTelemetry.TickCounter.Add(1, tags);
        ControlLoopTelemetry.TickDuration.Record(durationMs, tags);
    }

    public void Transition(string fromState, string toState)
    {
        var tags = With(
            new(ControlLoopTelemetry.TagFromState, fromState),
            new(ControlLoopTelemetry.TagToState, toState),
            new(ControlLoopTelemetry.TagLifecycleState, toState));
        ControlLoopTelemetry.TransitionCounter.Add(1, tags);
    }

    public void Retry(int attempt, string lifecycleState)
    {
        var tags = With(
            new(ControlLoopTelemetry.TagAttempt, attempt),
            new(ControlLoopTelemetry.TagLifecycleState, lifecycleState));
        ControlLoopTelemetry.RetryCounter.Add(1, tags);
    }

    public void Escalation(string tier, string lifecycleState)
    {
        var tags = With(
            new(ControlLoopTelemetry.TagTier, tier),
            new(ControlLoopTelemetry.TagLifecycleState, lifecycleState));
        ControlLoopTelemetry.EscalationCounter.Add(1, tags);
    }

    public void EvidencePublished(string evidenceKind, string lifecycleState)
    {
        var tags = With(
            new(ControlLoopTelemetry.TagEvidenceKind, evidenceKind),
            new(ControlLoopTelemetry.TagLifecycleState, lifecycleState));
        ControlLoopTelemetry.EvidenceCounter.Add(1, tags);
    }

    public void Attention(AttentionItem item, string lifecycleState)
    {
        ArgumentNullException.ThrowIfNull(item);
        var tags = With(
            new(ControlLoopTelemetry.TagSeverity, item.Severity),
            new(ControlLoopTelemetry.TagCode, item.Code),
            new(ControlLoopTelemetry.TagLifecycleState, lifecycleState));
        ControlLoopTelemetry.AttentionCounter.Add(1, tags);
    }

    /// <summary>
    /// Start an Activity span for a tick or transition. Caller disposes the
    /// returned activity to stop the span. Returns null if no listener is
    /// subscribed to <see cref="ControlLoopTelemetry.ActivitySource"/>.
    /// </summary>
    public Activity? StartActivity(string spanName, string lifecycleState)
    {
        var activity = ControlLoopTelemetry.ActivitySource.StartActivity(spanName, ActivityKind.Internal);
        if (activity is not null)
        {
            activity.SetTag(ControlLoopTelemetry.TagControlLoopKey, ControlLoopKey);
            activity.SetTag(ControlLoopTelemetry.TagActorType, ActorType);
            activity.SetTag(ControlLoopTelemetry.TagLifecycleState, lifecycleState);
        }
        return activity;
    }

    private KeyValuePair<string, object?>[] WithLifecycle(string lifecycleState) =>
        With(new KeyValuePair<string, object?>(ControlLoopTelemetry.TagLifecycleState, lifecycleState));

    private KeyValuePair<string, object?>[] With(params KeyValuePair<string, object?>[] extra)
    {
        var combined = new KeyValuePair<string, object?>[_baseTags.Length + extra.Length];
        Array.Copy(_baseTags, combined, _baseTags.Length);
        Array.Copy(extra, 0, combined, _baseTags.Length, extra.Length);
        return combined;
    }
}
