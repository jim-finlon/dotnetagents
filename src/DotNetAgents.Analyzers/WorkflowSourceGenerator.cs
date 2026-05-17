using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DotNetAgents.Analyzers;

/// <summary>
/// Source generator that creates visualization helpers and debug information for StateGraph workflows.
/// </summary>
[Generator]
public class WorkflowSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // For now, generate a simple helper class
        // Full implementation would analyze StateGraph usages using IncrementalValueProvider
        context.RegisterPostInitializationOutput(ctx =>
        {
            var source = GenerateWorkflowVisualizationHelper();
            ctx.AddSource("WorkflowVisualizationHelper.g.cs", source);
        });
    }

    private static string GenerateWorkflowVisualizationHelper()
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using DotNetAgents.Workflow.Graph;");
        sb.AppendLine();
        sb.AppendLine("namespace DotNetAgents.Analyzers.Generated;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Auto-generated visualization helper for StateGraph workflows.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public class WorkflowVisualizationMetadata");
        sb.AppendLine("{");
        sb.AppendLine("    public string? EntryPoint { get; set; }");
        sb.AppendLine("    public string[] ExitPoints { get; set; } = Array.Empty<string>();");
        sb.AppendLine("    public string[] Nodes { get; set; } = Array.Empty<string>();");
        sb.AppendLine("    public (string From, string To)[] Edges { get; set; } = Array.Empty<(string, string)>();");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
