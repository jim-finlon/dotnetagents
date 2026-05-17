namespace DotNetAgents.Agents.ControlLoops;

/// <summary>
/// Sink that a behavior tree calls to start a durable workflow run. Story
/// 2d0994f8. Service-agnostic — actual workflow engines (Workflow runtime,
/// MediatR Send, custom orchestrator) implement this. Trees never embed
/// orchestration logic; they trigger it through this seam.
/// </summary>
public interface IWorkflowTriggerSink
{
    /// <summary>Start a workflow run. Returns the assigned run id (or null when suppressed by dedup/governance).</summary>
    Task<Guid?> TriggerAsync(WorkflowTriggerRequest request, CancellationToken ct = default);
}

/// <summary>
/// Sink that a behavior tree calls to enqueue work for a queue/dispatcher.
/// Same pattern as <see cref="IWorkflowTriggerSink"/>: trees produce a
/// declarative request, the sink owns delivery.
/// </summary>
public interface IQueueTriggerSink
{
    /// <summary>Enqueue work. Returns the assigned message id (or null when suppressed).</summary>
    Task<Guid?> EnqueueAsync(QueueTriggerRequest request, CancellationToken ct = default);
}

public sealed record WorkflowTriggerRequest(
    string WorkflowKey,
    TriggerCorrelation Correlation,
    IReadOnlyDictionary<string, object?>? Inputs = null,
    string? DedupKey = null);

public sealed record QueueTriggerRequest(
    string QueueKey,
    TriggerCorrelation Correlation,
    IReadOnlyDictionary<string, object?>? Payload = null,
    string? DedupKey = null,
    int? PriorityHint = null);

/// <summary>
/// Correlation/evidence context stamped onto every trigger request. Story
/// 2d0994f8. Lets operators + downstream workflows answer "why was this
/// started?" without reverse-engineering the tree. Mirrors the shape of
/// <see cref="ControlLoopRunContext"/> from story 121a8afc so the same
/// (run id, story id, correlation id) flow through the platform.
/// </summary>
public sealed record TriggerCorrelation(
    string TriggeredBy,
    Guid? CorrelationId = null,
    Guid? ControlLoopRunId = null,
    string? StoryId = null,
    string? Reason = null,
    EvidenceReference? Evidence = null,
    DateTime? StampedAtUtc = null)
{
    /// <summary>Convenience: stamp now from a control-loop run context.</summary>
    public static TriggerCorrelation FromRunContext(ControlLoopRunContext run, string triggeredBy, string? reason = null, EvidenceReference? evidence = null) =>
        new(triggeredBy, run.CorrelationId, run.RunId, run.StoryId, reason, evidence, DateTime.UtcNow);
}

/// <summary>
/// In-memory <see cref="IWorkflowTriggerSink"/> for tests + dev. Captures
/// every trigger so tests can assert on what would have been started.
/// Honors dedup keys: a second trigger with the same DedupKey returns null
/// (suppressed) without recording a duplicate run.
/// </summary>
public sealed class InMemoryWorkflowTriggerSink : IWorkflowTriggerSink
{
    private readonly object _lock = new();
    private readonly List<RecordedTrigger> _triggers = new();
    private readonly HashSet<string> _seenDedupKeys = new(StringComparer.Ordinal);

    public IReadOnlyList<RecordedTrigger> Triggers
    {
        get { lock (_lock) return _triggers.ToArray(); }
    }

    public Task<Guid?> TriggerAsync(WorkflowTriggerRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        lock (_lock)
        {
            if (request.DedupKey is not null && !_seenDedupKeys.Add(request.DedupKey))
                return Task.FromResult<Guid?>(null);
            var runId = Guid.NewGuid();
            _triggers.Add(new RecordedTrigger(runId, request));
            return Task.FromResult<Guid?>(runId);
        }
    }

    public sealed record RecordedTrigger(Guid RunId, WorkflowTriggerRequest Request);
}

/// <summary>
/// In-memory <see cref="IQueueTriggerSink"/> for tests + dev. Same dedup
/// semantics as the workflow sink.
/// </summary>
public sealed class InMemoryQueueTriggerSink : IQueueTriggerSink
{
    private readonly object _lock = new();
    private readonly List<RecordedEnqueue> _enqueues = new();
    private readonly HashSet<string> _seenDedupKeys = new(StringComparer.Ordinal);

    public IReadOnlyList<RecordedEnqueue> Enqueues
    {
        get { lock (_lock) return _enqueues.ToArray(); }
    }

    public Task<Guid?> EnqueueAsync(QueueTriggerRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        lock (_lock)
        {
            if (request.DedupKey is not null && !_seenDedupKeys.Add(request.DedupKey))
                return Task.FromResult<Guid?>(null);
            var msgId = Guid.NewGuid();
            _enqueues.Add(new RecordedEnqueue(msgId, request));
            return Task.FromResult<Guid?>(msgId);
        }
    }

    public sealed record RecordedEnqueue(Guid MessageId, QueueTriggerRequest Request);
}
