using DotNetAgents.Workflow.Graph;
using System.Text;
using System.Text.Json;

namespace DotNetAgents.Workflow.Visualization;

/// <summary>
/// Service for visualizing workflow graphs in various formats.
/// </summary>
public class GraphVisualizationService : IGraphVisualizationService
{
    /// <inheritdoc/>
    public string GenerateDot<TState>(StateGraph<TState> graph) where TState : class
    {
        ArgumentNullException.ThrowIfNull(graph);

        var builder = new StringBuilder();
        builder.AppendLine("digraph Workflow {");
        builder.AppendLine("  rankdir=LR;");
        builder.AppendLine("  node [shape=box, style=rounded];");

        // Add nodes
        foreach (var node in graph.Nodes.Values)
        {
            var nodeId = EscapeDotId(node.Name);
            var label = EscapeDotLabel(node.Description ?? node.Name);
            var shape = graph.EntryPoint == node.Name
                ? "ellipse"
                : graph.ExitPoints.Contains(node.Name)
                    ? "doublecircle"
                    : "box";

            builder.AppendLine($"  {nodeId} [label=\"{label}\", shape={shape}];");
        }

        // Add edges
        foreach (var edge in graph.Edges)
        {
            var fromId = EscapeDotId(edge.From);
            var toId = EscapeDotId(edge.To);
            var label = edge.Condition != null
                ? $" [label=\"{EscapeDotLabel(edge.Description ?? "conditional")}\"]"
                : string.Empty;

            builder.AppendLine($"  {fromId} -> {toId}{label};");
        }

        builder.AppendLine("}");

        return builder.ToString();
    }

    /// <inheritdoc/>
    public string GenerateMermaid<TState>(StateGraph<TState> graph) where TState : class
    {
        ArgumentNullException.ThrowIfNull(graph);

        var builder = new StringBuilder();
        builder.AppendLine("graph LR");

        // Add nodes
        foreach (var node in graph.Nodes.Values)
        {
            var nodeId = EscapeMermaidId(node.Name);
            var label = EscapeMermaidLabel(node.Description ?? node.Name);
            var shape = graph.EntryPoint == node.Name
                ? "(({0}))"
                : graph.ExitPoints.Contains(node.Name)
                    ? "([{0}])"
                    : "[{0}]";

            builder.AppendLine($"  {nodeId}{string.Format(shape, label)}");
        }

        // Add edges
        foreach (var edge in graph.Edges)
        {
            var fromId = EscapeMermaidId(edge.From);
            var toId = EscapeMermaidId(edge.To);
            var label = edge.Condition != null
                ? $"|{EscapeMermaidLabel(edge.Description ?? "conditional")}|"
                : string.Empty;

            builder.AppendLine($"  {fromId} -->{label} {toId}");
        }

        return builder.ToString();
    }

    /// <inheritdoc/>
    public string GenerateJson<TState>(StateGraph<TState> graph) where TState : class
    {
        ArgumentNullException.ThrowIfNull(graph);

        var metadata = new GraphMetadata<TState>
        {
            EntryPoint = graph.EntryPoint,
            ExitPoints = graph.ExitPoints.ToList(),
            Nodes = graph.Nodes.Values.Select(n => new NodeMetadata
            {
                Name = n.Name,
                Description = n.Description
            }).ToList(),
            Edges = graph.Edges.Select(e => new EdgeMetadata
            {
                From = e.From,
                To = e.To,
                Description = e.Description,
                IsConditional = e.Condition != null
            }).ToList()
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(metadata, options);
    }

    private static string EscapeDotId(string id)
    {
        // Replace special characters with underscores
        return id.Replace("-", "_").Replace(" ", "_");
    }

    private static string EscapeDotLabel(string label)
    {
        // Escape quotes and newlines for DOT
        return label.Replace("\"", "\\\"").Replace("\n", "\\n");
    }

    private static string EscapeMermaidId(string id)
    {
        // Mermaid IDs should be alphanumeric with underscores
        return id.Replace("-", "_").Replace(" ", "_");
    }

    private static string EscapeMermaidLabel(string label)
    {
        // Escape special characters for Mermaid
        return label.Replace("\"", "&quot;").Replace("\n", "<br/>");
    }

    private class GraphMetadata<TState>
    {
        public string? EntryPoint { get; set; }
        public List<string> ExitPoints { get; set; } = new();
        public List<NodeMetadata> Nodes { get; set; } = new();
        public List<EdgeMetadata> Edges { get; set; } = new();
    }

    private class NodeMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    private class EdgeMetadata
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsConditional { get; set; }
    }
}
