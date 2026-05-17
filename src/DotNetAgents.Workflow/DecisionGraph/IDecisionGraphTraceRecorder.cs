namespace DotNetAgents.Workflow.DecisionGraph;

/// <summary>
/// Sink for the per-run + per-event trace emitted by <see cref="DecisionGraphRuntime"/>.
/// Story 67a5c613. The runtime is intentionally agnostic about persistence —
/// production callers (JARVIS) wire this to the <c>jarvis_decision_graph_runs</c>
/// + <c>jarvis_decision_graph_run_events</c> tables via a JARVIS-side adapter
/// over <see cref="IDecisionGraphRepository"/> (story 0838c66b). Tests use the
/// in-memory recorder below.
/// </summary>
public interface IDecisionGraphTraceRecorder
{
    Task RecordRunStartedAsync(DecisionGraphRunContext context, CancellationToken ct);
    Task RecordNodeStartedAsync(DecisionGraphRunContext context, int sequence, DecisionGraphNode node, CancellationToken ct);
    Task RecordNodeCompletedAsync(DecisionGraphRunContext context, int sequence, DecisionGraphNode node, NodeExecutionResult result, CancellationToken ct);
    Task RecordRunCompletedAsync(DecisionGraphRunContext context, string exitNodeId, long latencyMs, CancellationToken ct);
    Task RecordRunFailedAsync(DecisionGraphRunContext context, int sequence, string failedNodeId, string errorCode, string errorMessage, long latencyMs, CancellationToken ct);
}

/// <summary>
/// In-memory trace recorder that captures every event into a buffer. Story 67a5c613.
/// Intended for tests + dry-run scenarios that should not persist to the database.
/// Thread-safe through a lock on the underlying list.
/// </summary>
public sealed class InMemoryDecisionGraphTraceRecorder : IDecisionGraphTraceRecorder
{
    private readonly List<DecisionGraphTraceEvent> _events = new();
    private readonly object _lock = new();

    public IReadOnlyList<DecisionGraphTraceEvent> Events
    {
        get { lock (_lock) return _events.ToArray(); }
    }

    public Task RecordRunStartedAsync(DecisionGraphRunContext context, CancellationToken ct)
    {
        lock (_lock) _events.Add(new("run.started", 0, null, null, null));
        return Task.CompletedTask;
    }

    public Task RecordNodeStartedAsync(DecisionGraphRunContext context, int sequence, DecisionGraphNode node, CancellationToken ct)
    {
        lock (_lock) _events.Add(new("node.started", sequence, node.Id, node.Type.ToString(), null));
        return Task.CompletedTask;
    }

    public Task RecordNodeCompletedAsync(DecisionGraphRunContext context, int sequence, DecisionGraphNode node, NodeExecutionResult result, CancellationToken ct)
    {
        lock (_lock) _events.Add(new("node.completed", sequence, node.Id, node.Type.ToString(), result.Summary));
        return Task.CompletedTask;
    }

    public Task RecordRunCompletedAsync(DecisionGraphRunContext context, string exitNodeId, long latencyMs, CancellationToken ct)
    {
        lock (_lock) _events.Add(new("run.completed", 0, exitNodeId, null, $"latency={latencyMs}ms"));
        return Task.CompletedTask;
    }

    public Task RecordRunFailedAsync(DecisionGraphRunContext context, int sequence, string failedNodeId, string errorCode, string errorMessage, long latencyMs, CancellationToken ct)
    {
        lock (_lock) _events.Add(new("run.failed", sequence, failedNodeId, null, $"{errorCode}:{errorMessage}"));
        return Task.CompletedTask;
    }
}

public sealed record DecisionGraphTraceEvent(string EventType, int Sequence, string? NodeId, string? NodeType, string? Summary);
