// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Agents.ControlLoops;

/// <summary>
/// Contract a service implements (or composes) to publish its current control-loop
/// snapshot to operator + observability consumers. Story 121a8afc.
///
/// One method, returning the snapshot synchronously assembled from whatever the
/// service already tracks. Implementations should NOT do heavy I/O — the consumer
/// (heartbeat publisher, /health endpoint, dashboard query) calls this often.
/// </summary>
public interface IControlLoopSnapshotPublisher
{
    /// <summary>The stable key identifying this control loop. Same for every snapshot from a given loop.</summary>
    string ControlLoopKey { get; }

    /// <summary>Build the current snapshot. Cheap enough to call on a 1s tick.</summary>
    ControlLoopRunSnapshot CaptureSnapshot();
}

/// <summary>
/// Convenience builder for <see cref="ControlLoopRunSnapshot"/>. Services that
/// want to compose a snapshot fluently (rather than constructing the records
/// directly) can use this — it doesn't add behavior, just ergonomics.
/// </summary>
public sealed class ControlLoopSnapshotBuilder
{
    private readonly ControlLoopRunContext _runContext;
    private LifecycleStateSnapshot _lifecycle;
    private GovernancePosture _governance;
    private readonly List<AttentionItem> _attention = new();
    private readonly List<QueueDispatchSummary> _queues = new();
    private readonly List<EvidenceReference> _evidence = new();
    private string _status = "Healthy";
    private string _headline = "OK";
    private Dictionary<string, string>? _tags;

    public ControlLoopSnapshotBuilder(ControlLoopRunContext runContext, LifecycleStateSnapshot lifecycle, GovernancePosture governance)
    {
        _runContext = runContext;
        _lifecycle = lifecycle;
        _governance = governance;
    }

    public ControlLoopSnapshotBuilder WithStatus(string status, string headline) { _status = status; _headline = headline; return this; }
    public ControlLoopSnapshotBuilder Lifecycle(LifecycleStateSnapshot snapshot) { _lifecycle = snapshot; return this; }
    public ControlLoopSnapshotBuilder Governance(GovernancePosture posture) { _governance = posture; return this; }
    public ControlLoopSnapshotBuilder Attention(AttentionItem item) { _attention.Add(item); return this; }
    public ControlLoopSnapshotBuilder Queue(QueueDispatchSummary q) { _queues.Add(q); return this; }
    public ControlLoopSnapshotBuilder Evidence(EvidenceReference e) { _evidence.Add(e); return this; }
    public ControlLoopSnapshotBuilder Tag(string key, string value) { (_tags ??= new(StringComparer.Ordinal))[key] = value; return this; }

    public ControlLoopRunSnapshot Build() =>
        new(
            _runContext,
            new ControlLoopHealthSummary(
                _status,
                _headline,
                DateTime.UtcNow,
                _lifecycle,
                _governance,
                _attention.ToArray(),
                _queues.ToArray(),
                _evidence.ToArray()),
            _tags);
}
