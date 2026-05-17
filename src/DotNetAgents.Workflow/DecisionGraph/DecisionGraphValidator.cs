namespace DotNetAgents.Workflow.DecisionGraph;

/// <summary>
/// Pure rule-based validator for <see cref="DecisionGraphDefinition"/>. Story 55db0c7d.
/// All rejection rules from docs/requirements/jarvis-decision-graphs/INDEX.md
/// (Compiler Strategy section) are enforced here. Returns machine-readable errors
/// suitable for surfacing through the operator API + MCP tools.
/// </summary>
public sealed class DecisionGraphValidator
{
    public DecisionGraphValidationReport Validate(DecisionGraphDefinition graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        var errors = new List<DecisionGraphValidationError>();

        if (!string.Equals(graph.SchemaVersion, DecisionGraphDefinition.CurrentSchemaVersion, StringComparison.Ordinal))
            errors.Add(new("schemaVersion.mismatch", $"Expected schemaVersion '{DecisionGraphDefinition.CurrentSchemaVersion}', got '{graph.SchemaVersion}'."));

        if (string.IsNullOrWhiteSpace(graph.GraphKey))
            errors.Add(new("graphKey.missing", "graphKey is required."));
        if (string.IsNullOrWhiteSpace(graph.Version))
            errors.Add(new("version.missing", "version is required."));

        if (graph.Nodes.Count == 0)
        {
            errors.Add(new("nodes.empty", "Graph must contain at least one node."));
            return new DecisionGraphValidationReport(false, errors);
        }

        var nodeIds = new HashSet<string>(StringComparer.Ordinal);
        var dupNodeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var n in graph.Nodes)
        {
            if (string.IsNullOrWhiteSpace(n.Id))
            {
                errors.Add(new("node.id.missing", "Every node requires a non-empty id."));
                continue;
            }
            if (!nodeIds.Add(n.Id))
                dupNodeIds.Add(n.Id);
        }
        foreach (var dup in dupNodeIds)
            errors.Add(new("node.id.duplicate", $"Duplicate node id '{dup}'.") { NodeId = dup });

        if (string.IsNullOrWhiteSpace(graph.EntryNodeId))
            errors.Add(new("entryNodeId.missing", "entryNodeId is required."));
        else if (!nodeIds.Contains(graph.EntryNodeId))
            errors.Add(new("entryNodeId.unknown", $"entryNodeId '{graph.EntryNodeId}' does not match any node.") { NodeId = graph.EntryNodeId });

        if (graph.ExitNodeIds.Count == 0)
            errors.Add(new("exitNodeIds.empty", "exitNodeIds must contain at least one node id."));
        foreach (var exit in graph.ExitNodeIds)
            if (!nodeIds.Contains(exit))
                errors.Add(new("exitNodeIds.unknown", $"exit node '{exit}' does not match any node.") { NodeId = exit });

        // Edge integrity
        foreach (var e in graph.Edges)
        {
            if (!nodeIds.Contains(e.From))
                errors.Add(new("edge.from.unknown", $"Edge from '{e.From}' references unknown node.") { NodeId = e.From });
            if (!nodeIds.Contains(e.To))
                errors.Add(new("edge.to.unknown", $"Edge to '{e.To}' references unknown node.") { NodeId = e.To });
        }

        // Reachability from entry (skip if entry unknown)
        if (!string.IsNullOrWhiteSpace(graph.EntryNodeId) && nodeIds.Contains(graph.EntryNodeId))
        {
            var reachable = ComputeReachable(graph.EntryNodeId, graph.Edges);
            foreach (var nid in nodeIds)
                if (!reachable.Contains(nid) && !graph.ExitNodeIds.Contains(nid) && nid != graph.EntryNodeId)
                    errors.Add(new("node.unreachable", $"Node '{nid}' is unreachable from entry '{graph.EntryNodeId}'.") { NodeId = nid });
        }

        // Cycle detection — every cycle-participating edge must declare loopBound
        var cycles = FindCycles(graph.Edges, nodeIds);
        foreach (var cycle in cycles)
        {
            // Every edge in the cycle must have a loopBound; otherwise reject.
            foreach (var (from, to) in cycle)
            {
                var edge = graph.Edges.FirstOrDefault(e => e.From == from && e.To == to);
                if (edge is not null && (edge.LoopBound is null || edge.LoopBound <= 0))
                {
                    errors.Add(new("cycle.unbounded", $"Cycle contains edge {from}->{to} without loopBound.") { NodeId = from });
                }
            }
        }

        // Tool-call nodes: declared tool must be in allowlist
        foreach (var n in graph.Nodes.Where(n => n.Type == DecisionGraphNodeType.ToolCall))
        {
            var toolName = TryGetConfigString(n, "toolName")
                          ?? TryGetConfigString(n, "selectedToolVariable"); // variable indirection is OK; only literal toolName forces allowlist check
            if (toolName is not null && !TryGetConfigString(n, "selectedToolVariable").HasValue() &&
                !graph.ToolAllowlist.Tools.Contains(toolName, StringComparer.Ordinal))
            {
                errors.Add(new("tool.outsideAllowlist", $"Node '{n.Id}' calls tool '{toolName}' which is not in toolAllowlist.tools.") { NodeId = n.Id });
            }
        }

        // Memory.retrieve nodes: scopes must be in allowlist
        foreach (var n in graph.Nodes.Where(n => n.Type == DecisionGraphNodeType.MemoryRetrieve))
        {
            var scopes = TryGetConfigStringArray(n, "scopes");
            foreach (var scope in scopes)
                if (!graph.ToolAllowlist.MemoryScopes.Contains(scope, StringComparer.Ordinal))
                    errors.Add(new("memory.scopeOutsideAllowlist", $"Node '{n.Id}' retrieves memory scope '{scope}' which is not in toolAllowlist.memoryScopes.") { NodeId = n.Id });
        }

        // LLM-using nodes: taskFamily must be in allowlist
        foreach (var n in graph.Nodes.Where(IsLlmUsingNode))
        {
            var family = TryGetConfigString(n, "taskFamily");
            if (family is not null && !graph.ToolAllowlist.LlmTaskFamilies.Contains(family, StringComparer.Ordinal))
                errors.Add(new("llm.taskFamilyOutsideAllowlist", $"Node '{n.Id}' uses LLM task family '{family}' which is not in toolAllowlist.llmTaskFamilies.") { NodeId = n.Id });
        }

        // Mutation paths: any node with isMutation=true must be reached only via a policy.gate edge
        var mutationNodes = graph.Nodes.Where(n => n.IsMutation).Select(n => n.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var muId in mutationNodes)
        {
            var incoming = graph.Edges.Where(e => e.To == muId).ToList();
            if (incoming.Count == 0) continue;
            var allGated = incoming.All(e =>
            {
                var src = graph.Nodes.FirstOrDefault(n => n.Id == e.From);
                return src is not null && src.Type == DecisionGraphNodeType.PolicyGate;
            });
            if (!allGated)
                errors.Add(new("mutation.unguarded", $"Mutation node '{muId}' has at least one incoming edge that does not originate from a policy.gate node.") { NodeId = muId });
        }

        return new DecisionGraphValidationReport(errors.Count == 0, errors);
    }

    private static bool IsLlmUsingNode(DecisionGraphNode n) =>
        n.Type is DecisionGraphNodeType.IntentClassify
            or DecisionGraphNodeType.ToolSelect
            or DecisionGraphNodeType.LlmReason
            or DecisionGraphNodeType.ResponseCompose
            or DecisionGraphNodeType.QualityScore;

    private static HashSet<string> ComputeReachable(string entry, List<DecisionGraphEdge> edges)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal) { entry };
        var stack = new Stack<string>();
        stack.Push(entry);
        while (stack.TryPop(out var current))
            foreach (var e in edges.Where(x => x.From == current))
                if (seen.Add(e.To))
                    stack.Push(e.To);
        return seen;
    }

    private static List<List<(string From, string To)>> FindCycles(List<DecisionGraphEdge> edges, HashSet<string> nodeIds)
    {
        // Tarjan-style DFS that records edges of each cycle. We don't enumerate all cycles —
        // we just return one representative cycle per back-edge we find. That's enough to
        // require loopBound on every back-edge.
        var cycles = new List<List<(string, string)>>();
        var color = new Dictionary<string, int>(); // 0=white,1=gray,2=black
        var parent = new Dictionary<string, string?>();
        foreach (var nid in nodeIds) { color[nid] = 0; parent[nid] = null; }

        void Dfs(string u)
        {
            color[u] = 1;
            foreach (var e in edges.Where(x => x.From == u))
            {
                var v = e.To;
                if (!color.ContainsKey(v)) continue;
                if (color[v] == 0)
                {
                    parent[v] = u;
                    Dfs(v);
                }
                else if (color[v] == 1)
                {
                    // back-edge u->v; recover cycle path v..u + edge u->v
                    var cyclePath = new List<(string, string)> { (u, v) };
                    var cur = u;
                    while (cur != v && cur is not null)
                    {
                        var p = parent[cur];
                        if (p is null) break;
                        cyclePath.Add((p, cur));
                        cur = p;
                    }
                    cycles.Add(cyclePath);
                }
            }
            color[u] = 2;
        }

        foreach (var nid in nodeIds)
            if (color[nid] == 0) Dfs(nid);

        return cycles;
    }

    private static string? TryGetConfigString(DecisionGraphNode n, string key)
    {
        if (!n.Config.TryGetValue(key, out var v) || v is null) return null;
        if (v is string s) return s;
        if (v is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.String) return je.GetString();
        return v.ToString();
    }

    private static IReadOnlyList<string> TryGetConfigStringArray(DecisionGraphNode n, string key)
    {
        if (!n.Config.TryGetValue(key, out var v) || v is null) return Array.Empty<string>();
        if (v is IEnumerable<string> ses) return ses.ToArray();
        if (v is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Array)
            return je.EnumerateArray().Where(x => x.ValueKind == System.Text.Json.JsonValueKind.String).Select(x => x.GetString()!).ToArray();
        return Array.Empty<string>();
    }
}

internal static class StringExtensions
{
    public static bool HasValue(this string? s) => !string.IsNullOrWhiteSpace(s);
}

public sealed record DecisionGraphValidationReport(bool IsValid, IReadOnlyList<DecisionGraphValidationError> Errors);

public sealed record DecisionGraphValidationError(string Code, string Message)
{
    public string? NodeId { get; init; }
}
