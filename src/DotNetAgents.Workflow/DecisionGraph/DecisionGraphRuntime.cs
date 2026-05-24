// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;

namespace DotNetAgents.Workflow.DecisionGraph;

/// <summary>
/// Runtime executor for a compiled JARVIS decision graph. Story 67a5c613.
/// Walks edges from <see cref="DecisionGraphDefinition.EntryNodeId"/> to one of
/// the declared <see cref="DecisionGraphDefinition.ExitNodeIds"/>, dispatching
/// each node to a registered <see cref="INodeExecutor"/>. The runtime owns:
///   - graph state (node outputs, scoring, intent, tool results, policy decisions)
///   - edge predicate evaluation via <see cref="EdgeEvaluator"/>
///   - timing + sequence + per-node event emission via <see cref="IDecisionGraphTraceRecorder"/>
///   - safety policy enforcement (timeout, max loop iterations)
///
/// All node-type-specific behavior lives in INodeExecutor implementations. The
/// default <see cref="NoOpNodeExecutorRegistry"/> stubs every node type so a
/// graph can be exercised end-to-end before the JARVIS-side LLM/MCP/memory
/// executors land.
/// </summary>
public sealed class DecisionGraphRuntime
{
    private readonly INodeExecutorRegistry _executors;
    private readonly IDecisionGraphTraceRecorder _trace;

    public DecisionGraphRuntime(INodeExecutorRegistry executors, IDecisionGraphTraceRecorder trace)
    {
        _executors = executors ?? throw new ArgumentNullException(nameof(executors));
        _trace = trace ?? throw new ArgumentNullException(nameof(trace));
    }

    public async Task<DecisionGraphRunOutcome> ExecuteAsync(
        CompiledDecisionGraph compiled,
        DecisionGraphRunInput input,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(compiled);
        ArgumentNullException.ThrowIfNull(input);

        var graph = compiled.Definition;
        var context = new DecisionGraphRunContext(graph, input);
        var sw = Stopwatch.StartNew();

        await _trace.RecordRunStartedAsync(context, ct).ConfigureAwait(false);

        var sequence = 0;
        var current = graph.EntryNodeId;
        var loopCounters = new Dictionary<string, int>();
        var deadline = DateTime.UtcNow.AddMilliseconds(graph.SafetyPolicy.TimeoutMs);

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            if (DateTime.UtcNow > deadline)
                return await FailAsync(context, sequence, current, "timeout", $"Graph timed out after {graph.SafetyPolicy.TimeoutMs}ms.", sw, ct).ConfigureAwait(false);

            var node = compiled.NodesById[current];
            sequence++;

            await _trace.RecordNodeStartedAsync(context, sequence, node, ct).ConfigureAwait(false);

            NodeExecutionResult result;
            try
            {
                var executor = _executors.Resolve(node.Type);
                result = await executor.ExecuteAsync(node, context, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return await FailAsync(context, sequence, current, "node.execution.error", ex.Message, sw, ct).ConfigureAwait(false);
            }

            context.RecordNodeOutput(node.Id, result);

            sequence++;
            await _trace.RecordNodeCompletedAsync(context, sequence, node, result, ct).ConfigureAwait(false);

            if (graph.ExitNodeIds.Contains(node.Id))
            {
                sw.Stop();
                await _trace.RecordRunCompletedAsync(context, node.Id, sw.ElapsedMilliseconds, ct).ConfigureAwait(false);
                return new DecisionGraphRunOutcome(
                    Succeeded: true,
                    ExitNodeId: node.Id,
                    LatencyMs: (int)sw.ElapsedMilliseconds,
                    State: context.State,
                    Error: null);
            }

            var nextEdge = compiled.OutgoingEdges
                .GetValueOrDefault(node.Id, Array.Empty<DecisionGraphEdge>())
                .FirstOrDefault(e => EdgeEvaluator.Evaluate(e.Condition, context, node, result));

            if (nextEdge is null)
                return await FailAsync(context, sequence, current, "edge.noMatch", $"No outgoing edge from '{node.Id}' matched the current state.", sw, ct).ConfigureAwait(false);

            if (nextEdge.LoopBound is not null)
            {
                var key = $"{nextEdge.From}->{nextEdge.To}";
                loopCounters[key] = loopCounters.GetValueOrDefault(key, 0) + 1;
                if (loopCounters[key] > nextEdge.LoopBound.Value)
                    return await FailAsync(context, sequence, current, "loop.bounded.exceeded", $"Edge {key} exceeded loopBound={nextEdge.LoopBound.Value}.", sw, ct).ConfigureAwait(false);
                if (loopCounters[key] > graph.SafetyPolicy.MaxLoopIterations)
                    return await FailAsync(context, sequence, current, "loop.maxIterations.exceeded", $"Edge {key} exceeded safetyPolicy.maxLoopIterations={graph.SafetyPolicy.MaxLoopIterations}.", sw, ct).ConfigureAwait(false);
            }

            current = nextEdge.To;
        }
    }

    private async Task<DecisionGraphRunOutcome> FailAsync(
        DecisionGraphRunContext context,
        int sequence,
        string nodeId,
        string errorCode,
        string errorMessage,
        Stopwatch sw,
        CancellationToken ct)
    {
        sw.Stop();
        await _trace.RecordRunFailedAsync(context, sequence, nodeId, errorCode, errorMessage, sw.ElapsedMilliseconds, ct).ConfigureAwait(false);
        return new DecisionGraphRunOutcome(
            Succeeded: false,
            ExitNodeId: null,
            LatencyMs: (int)sw.ElapsedMilliseconds,
            State: context.State,
            Error: new DecisionGraphRunError(errorCode, errorMessage, nodeId));
    }
}

/// <summary>Compiled artifact returned by <see cref="DecisionGraphCompiler"/> — definition + indexes for fast lookup during execution.</summary>
public sealed class CompiledDecisionGraph
{
    public required DecisionGraphDefinition Definition { get; init; }
    public required IReadOnlyDictionary<string, DecisionGraphNode> NodesById { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyList<DecisionGraphEdge>> OutgoingEdges { get; init; }
}

public sealed record DecisionGraphRunInput(
    Guid? CommandId,
    Guid? ConversationSessionId,
    Guid? UserId,
    string Transcript,
    IReadOnlyDictionary<string, object?>? InitialState = null);

public sealed record DecisionGraphRunOutcome(
    bool Succeeded,
    string? ExitNodeId,
    int LatencyMs,
    IReadOnlyDictionary<string, object?> State,
    DecisionGraphRunError? Error);

public sealed record DecisionGraphRunError(string Code, string Message, string NodeId);

/// <summary>Mutable per-run state. Threaded through every node executor + edge evaluator.</summary>
public sealed class DecisionGraphRunContext
{
    public DecisionGraphDefinition Graph { get; }
    public DecisionGraphRunInput Input { get; }
    public Dictionary<string, object?> State { get; }
    public Dictionary<string, NodeExecutionResult> NodeResults { get; } = new(StringComparer.Ordinal);

    public DecisionGraphRunContext(DecisionGraphDefinition graph, DecisionGraphRunInput input)
    {
        Graph = graph;
        Input = input;
        State = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["transcript"] = input.Transcript,
            ["commandId"] = input.CommandId,
            ["conversationSessionId"] = input.ConversationSessionId,
            ["userId"] = input.UserId,
        };
        if (input.InitialState is not null)
            foreach (var (k, v) in input.InitialState) State[k] = v;
    }

    public void RecordNodeOutput(string nodeId, NodeExecutionResult result)
    {
        NodeResults[nodeId] = result;
        if (result.Outputs is null) return;
        foreach (var (k, v) in result.Outputs) State[k] = v;
    }
}
