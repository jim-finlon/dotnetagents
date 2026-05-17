namespace DotNetAgents.Workflow.DecisionGraph;

/// <summary>
/// Pluggable node executor. Story 67a5c613. One implementation per
/// <see cref="DecisionGraphNodeType"/>. The runtime resolves an executor by
/// node type via <see cref="INodeExecutorRegistry"/> and lets it produce a
/// <see cref="NodeExecutionResult"/> that the runtime then merges into the
/// run context's state dictionary.
/// </summary>
public interface INodeExecutor
{
    Task<NodeExecutionResult> ExecuteAsync(DecisionGraphNode node, DecisionGraphRunContext context, CancellationToken ct);
}

public interface INodeExecutorRegistry
{
    INodeExecutor Resolve(DecisionGraphNodeType nodeType);
}

/// <summary>
/// Result the executor returns. <see cref="Outputs"/> are merged into the run
/// context state. <see cref="ToolName"/> / <see cref="PolicyDecision"/> / etc.
/// surface in the run-event row for observability.
/// </summary>
public sealed record NodeExecutionResult(
    string? Summary,
    IReadOnlyDictionary<string, object?>? Outputs = null,
    string? ToolName = null,
    string? ToolCallId = null,
    string? PolicyDecision = null,
    string? ModelProviderKey = null,
    string? ModelName = null,
    string? Error = null)
{
    public static NodeExecutionResult Empty(string? summary = null) => new(summary);
}

/// <summary>
/// Default executor that records "noop" semantics for any node type. Story
/// 67a5c613. Lets the runtime + persistence layer be exercised end-to-end
/// before the JARVIS-side LLM/MCP/memory implementations land. Each node type
/// returns deterministic stub data so edge predicates evaluate consistently.
/// </summary>
public sealed class NoOpNodeExecutor : INodeExecutor
{
    public Task<NodeExecutionResult> ExecuteAsync(DecisionGraphNode node, DecisionGraphRunContext context, CancellationToken ct)
    {
        var outputs = new Dictionary<string, object?>(StringComparer.Ordinal);
        string? toolName = null;
        string? policyDecision = null;
        string? modelProviderKey = null;
        string? modelName = null;

        switch (node.Type)
        {
            case DecisionGraphNodeType.IntentClassify:
                outputs["intent"] = new { domain = "noop", phase = "noop", confidence = 0.0 };
                modelProviderKey = "noop";
                modelName = "noop-intent";
                break;
            case DecisionGraphNodeType.MemoryRetrieve:
                outputs["memoryItems"] = Array.Empty<object>();
                break;
            case DecisionGraphNodeType.ToolSelect:
                outputs["selectedTool"] = new { name = "noop", required = false };
                modelProviderKey = "noop";
                modelName = "noop-tool-planner";
                break;
            case DecisionGraphNodeType.ToolCall:
                outputs["toolResult"] = new { success = true, payload = (object?)null };
                toolName = "noop";
                break;
            case DecisionGraphNodeType.PolicyGate:
                outputs["policy"] = "allowed";
                policyDecision = "allowed";
                break;
            case DecisionGraphNodeType.LlmReason:
                outputs["reasoning"] = "noop";
                modelProviderKey = "noop";
                modelName = "noop-reasoning";
                break;
            case DecisionGraphNodeType.ResponseCompose:
                outputs["spokenResponse"] = "(noop response)";
                outputs["displayResponse"] = "(noop response)";
                modelProviderKey = "noop";
                modelName = "noop-composer";
                break;
            case DecisionGraphNodeType.StateTransition:
                // No state change — pure marker.
                break;
            case DecisionGraphNodeType.QualityScore:
                outputs["score"] = 1.0;
                break;
            case DecisionGraphNodeType.HumanConfirm:
                outputs["confirmation"] = "auto-allowed-noop";
                break;
            case DecisionGraphNodeType.SubgraphInvoke:
                outputs["subgraphResult"] = new { invoked = false, reason = "noop" };
                break;
        }

        return Task.FromResult(new NodeExecutionResult(
            Summary: $"noop:{node.Type}",
            Outputs: outputs,
            ToolName: toolName,
            PolicyDecision: policyDecision,
            ModelProviderKey: modelProviderKey,
            ModelName: modelName));
    }
}

/// <summary>Registry that returns the same singleton <see cref="NoOpNodeExecutor"/> for every node type.</summary>
public sealed class NoOpNodeExecutorRegistry : INodeExecutorRegistry
{
    private static readonly NoOpNodeExecutor Singleton = new();
    public INodeExecutor Resolve(DecisionGraphNodeType nodeType) => Singleton;
}

/// <summary>
/// Map-backed registry. Real callers (JARVIS) register one INodeExecutor per
/// node type; missing types fall back to <see cref="NoOpNodeExecutor"/>.
/// </summary>
public sealed class NodeExecutorRegistry : INodeExecutorRegistry
{
    private static readonly NoOpNodeExecutor Fallback = new();
    private readonly Dictionary<DecisionGraphNodeType, INodeExecutor> _map = new();

    public NodeExecutorRegistry Register(DecisionGraphNodeType type, INodeExecutor executor)
    {
        ArgumentNullException.ThrowIfNull(executor);
        _map[type] = executor;
        return this;
    }

    public INodeExecutor Resolve(DecisionGraphNodeType nodeType) =>
        _map.TryGetValue(nodeType, out var ex) ? ex : Fallback;
}
