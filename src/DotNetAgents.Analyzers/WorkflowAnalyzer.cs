using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace DotNetAgents.Analyzers;

/// <summary>
/// Analyzer for validating StateGraph workflow definitions at compile time.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class WorkflowAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DNA001";
    private const string Category = "DotNetAgents.Workflow";

    private static readonly DiagnosticDescriptor MissingEntryPointRule = new(
        DiagnosticId + "01",
        "Workflow graph must have an entry point",
        // RS1032: multi-sentence message must end with a trailing period.
        "StateGraph '{0}' does not have an entry point set. Call SetEntryPoint() before Build().",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "All workflow graphs must have an entry point.");

    private static readonly DiagnosticDescriptor MissingExitPointRule = new(
        DiagnosticId + "02",
        "Workflow graph must have at least one exit point",
        // RS1032: multi-sentence message must end with a trailing period.
        "StateGraph '{0}' does not have any exit points. Call AddExitPoint() before Build().",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "All workflow graphs must have at least one exit point.");

    private static readonly DiagnosticDescriptor UnreachableNodeRule = new(
        DiagnosticId + "03",
        "Workflow graph contains unreachable nodes",
        "StateGraph '{0}' contains unreachable node(s): {1}",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "All nodes in a workflow graph should be reachable from the entry point.");

    private static readonly DiagnosticDescriptor NodeWithoutOutgoingEdgesRule = new(
        DiagnosticId + "04",
        "Node has no outgoing edges and is not an exit point",
        // RS1032: single-sentence message must NOT end with a trailing period.
        "Node '{0}' in StateGraph '{1}' has no outgoing edges and is not marked as an exit point",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Nodes without outgoing edges should be marked as exit points.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            MissingEntryPointRule,
            MissingExitPointRule,
            UnreachableNodeRule,
            NodeWithoutOutgoingEdgesRule);

    public override void Initialize(AnalysisContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeStateGraph, SyntaxKind.InvocationExpression);
    }

    private void AnalyzeStateGraph(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation)
            return;

        // Check if this is a StateGraph.Build() call
        if (!IsStateGraphBuildCall(invocation, context))
            return;

        // Find the StateGraph variable or field
        var graphVariable = FindStateGraphVariable(invocation, context);
        if (graphVariable == null)
            return;

        // Analyze the graph construction
        AnalyzeGraphConstruction(graphVariable, invocation, context);
    }

    private static bool IsStateGraphBuildCall(InvocationExpressionSyntax invocation, SyntaxNodeAnalysisContext context)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        if (memberAccess.Name.Identifier.ValueText != "Build")
            return false;

        var symbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol;
        if (symbol == null)
            return false;

        // Check if it's a StateGraph<T>.Build() method
        var containingType = symbol.ContainingType;
        return containingType.Name == "StateGraph" && containingType.IsGenericType;
    }

    private static SyntaxNode? FindStateGraphVariable(InvocationExpressionSyntax invocation, SyntaxNodeAnalysisContext context)
    {
        // Try to find the StateGraph instance from the invocation
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess.Expression;
        }

        return null;
    }

    private void AnalyzeGraphConstruction(SyntaxNode? graphVariable, InvocationExpressionSyntax buildCall, SyntaxNodeAnalysisContext context)
    {
        if (graphVariable == null)
            return;

        // Find all method calls on this graph variable
        var graphConstruction = FindGraphConstruction(graphVariable, buildCall, context);

        if (graphConstruction == null)
            return;

        var hasEntryPoint = graphConstruction.HasEntryPoint;
        var hasExitPoint = graphConstruction.HasExitPoint;
        var nodes = graphConstruction.Nodes;
        var edges = graphConstruction.Edges;

        var graphName = graphVariable.ToString();

        // Check for missing entry point
        if (!hasEntryPoint)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                MissingEntryPointRule,
                buildCall.GetLocation(),
                graphName));
        }

        // Check for missing exit points
        if (!hasExitPoint)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                MissingExitPointRule,
                buildCall.GetLocation(),
                graphName));
        }

        // Check for nodes without outgoing edges (simplified check)
        // Full analysis would require more sophisticated flow analysis
        foreach (var node in nodes)
        {
            var hasOutgoingEdge = edges.Any(e => e.From == node);
            var isExitPoint = graphConstruction.ExitPoints.Contains(node);

            if (!hasOutgoingEdge && !isExitPoint)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    NodeWithoutOutgoingEdgesRule,
                    buildCall.GetLocation(),
                    node,
                    graphName));
            }
        }
    }

    private GraphConstruction? FindGraphConstruction(SyntaxNode? graphVariable, SyntaxNode buildCall, SyntaxNodeAnalysisContext context)
    {
        if (graphVariable == null)
            return null;

        // Find the parent statement or expression that contains the graph construction
        var parent = buildCall.Parent;
        while (parent != null)
        {
            if (parent is VariableDeclaratorSyntax variableDeclarator)
            {
                return AnalyzeVariableInitializer(variableDeclarator, context);
            }

            if (parent is AssignmentExpressionSyntax assignment)
            {
                return AnalyzeAssignment(assignment, context);
            }

            parent = parent.Parent;
        }

        return null;
    }

    private GraphConstruction? AnalyzeVariableInitializer(VariableDeclaratorSyntax declarator, SyntaxNodeAnalysisContext context)
    {
        if (declarator.Initializer?.Value is not ExpressionSyntax initializer)
            return null;

        return AnalyzeExpressionChain(initializer, context);
    }

    private GraphConstruction? AnalyzeAssignment(AssignmentExpressionSyntax assignment, SyntaxNodeAnalysisContext context)
    {
        return AnalyzeExpressionChain(assignment.Right, context);
    }

    private GraphConstruction? AnalyzeExpressionChain(ExpressionSyntax expression, SyntaxNodeAnalysisContext context)
    {
        var construction = new GraphConstruction();

        // Walk through method call chain
        var current = expression;
        while (current != null)
        {
            if (current is InvocationExpressionSyntax invocation)
            {
                AnalyzeMethodCall(invocation, construction, context);

                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    current = memberAccess.Expression;
                }
                else
                {
                    break;
                }
            }
            else if (current is ObjectCreationExpressionSyntax objectCreation)
            {
                // Found the new StateGraph<T>() call
                break;
            }
            else
            {
                break;
            }
        }

        return construction;
    }

    private void AnalyzeMethodCall(InvocationExpressionSyntax invocation, GraphConstruction construction, SyntaxNodeAnalysisContext context)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var methodName = memberAccess.Name.Identifier.ValueText;

        switch (methodName)
        {
            case "AddNode":
                if (invocation.ArgumentList.Arguments.Count > 0 &&
                    invocation.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal)
                {
                    var nodeName = literal.Token.ValueText;
                    construction.Nodes.Add(nodeName);
                }
                break;

            case "AddEdge":
                if (invocation.ArgumentList.Arguments.Count >= 2)
                {
                    var fromArg = invocation.ArgumentList.Arguments[0].Expression;
                    var toArg = invocation.ArgumentList.Arguments[1].Expression;

                    if (fromArg is LiteralExpressionSyntax fromLiteral &&
                        toArg is LiteralExpressionSyntax toLiteral)
                    {
                        construction.Edges.Add((fromLiteral.Token.ValueText, toLiteral.Token.ValueText));
                    }
                }
                break;

            case "SetEntryPoint":
                construction.HasEntryPoint = true;
                break;

            case "AddExitPoint":
                construction.HasExitPoint = true;
                if (invocation.ArgumentList.Arguments.Count > 0 &&
                    invocation.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax exitLiteral)
                {
                    construction.ExitPoints.Add(exitLiteral.Token.ValueText);
                }
                break;
        }
    }

    private class GraphConstruction
    {
        public bool HasEntryPoint { get; set; }
        public bool HasExitPoint { get; set; }
        public HashSet<string> Nodes { get; } = new();
        public List<(string From, string To)> Edges { get; } = new();
        public HashSet<string> ExitPoints { get; } = new();
    }
}
