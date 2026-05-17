using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace DotNetAgents.Analyzers;

/// <summary>
/// Analyzer for validating StateMachine definitions at compile time.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class StateMachineAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DNA002";
    private const string Category = "DotNetAgents.StateMachine";

    private static readonly DiagnosticDescriptor MissingInitialStateRule = new(
        DiagnosticId + "01",
        "State machine must have an initial state",
        "StateMachine '{0}' does not have an initial state defined",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "All state machines must have an initial state.");

    private static readonly DiagnosticDescriptor InvalidTransitionRule = new(
        DiagnosticId + "02",
        "State machine transition references non-existent state",
        // RS1032: single-sentence message must NOT end with a trailing period.
        "StateMachine '{0}' has a transition from '{1}' to '{2}', but state '{3}' does not exist",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "All state transitions must reference existing states.");

    private static readonly DiagnosticDescriptor UnreachableStateRule = new(
        DiagnosticId + "03",
        "State machine contains unreachable states",
        "StateMachine '{0}' contains unreachable state(s): {1}",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "All states in a state machine should be reachable from the initial state.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            MissingInitialStateRule,
            InvalidTransitionRule,
            UnreachableStateRule);

    public override void Initialize(AnalysisContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeStateMachine, SyntaxKind.InvocationExpression);
    }

    private void AnalyzeStateMachine(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation)
            return;

        // Check if this is a StateMachineBuilder.Build() call
        if (!IsStateMachineBuildCall(invocation, context))
            return;

        // Find the StateMachineBuilder variable or field
        var builderVariable = FindStateMachineBuilderVariable(invocation, context);
        if (builderVariable == null)
            return;

        // Analyze the state machine construction
        AnalyzeStateMachineConstruction(builderVariable, invocation, context);
    }

    private static bool IsStateMachineBuildCall(InvocationExpressionSyntax invocation, SyntaxNodeAnalysisContext context)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        if (memberAccess.Name.Identifier.ValueText != "Build")
            return false;

        var symbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol;
        if (symbol == null)
            return false;

        // Check if it's a StateMachineBuilder<T>.Build() method
        var containingType = symbol.ContainingType;
        return containingType.Name == "StateMachineBuilder" && containingType.IsGenericType;
    }

    private static SyntaxNode? FindStateMachineBuilderVariable(InvocationExpressionSyntax invocation, SyntaxNodeAnalysisContext context)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess.Expression;
        }

        return null;
    }

    private void AnalyzeStateMachineConstruction(SyntaxNode? builderVariable, InvocationExpressionSyntax buildCall, SyntaxNodeAnalysisContext context)
    {
        if (builderVariable == null)
            return;

        var stateMachineConstruction = FindStateMachineConstruction(builderVariable, buildCall, context);

        if (stateMachineConstruction == null)
            return;

        var hasInitialState = stateMachineConstruction.HasInitialState;
        var states = stateMachineConstruction.States;
        var transitions = stateMachineConstruction.Transitions;

        var stateMachineName = builderVariable.ToString();

        // Check for missing initial state
        if (!hasInitialState && states.Count > 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                MissingInitialStateRule,
                buildCall.GetLocation(),
                stateMachineName));
        }

        // Check for invalid transitions
        foreach (var (from, to) in transitions)
        {
            if (!states.Contains(from))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    InvalidTransitionRule,
                    buildCall.GetLocation(),
                    stateMachineName,
                    from,
                    to,
                    from));
            }

            if (!states.Contains(to))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    InvalidTransitionRule,
                    buildCall.GetLocation(),
                    stateMachineName,
                    from,
                    to,
                    to));
            }
        }

        // Check for unreachable states (simplified - would need full graph analysis)
        if (hasInitialState && states.Count > 0)
        {
            var initialState = stateMachineConstruction.InitialState;
            var reachable = GetReachableStates(initialState, transitions);
            var unreachable = states.Except(reachable).ToList();

            if (unreachable.Count > 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    UnreachableStateRule,
                    buildCall.GetLocation(),
                    stateMachineName,
                    string.Join(", ", unreachable)));
            }
        }
    }

    private HashSet<string> GetReachableStates(string initialState, List<(string From, string To)> transitions)
    {
        var reachable = new HashSet<string> { initialState };
        var queue = new Queue<string>();
        queue.Enqueue(initialState);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var nextStates = transitions
                .Where(t => t.From == current)
                .Select(t => t.To)
                .Where(s => !reachable.Contains(s));

            foreach (var next in nextStates)
            {
                reachable.Add(next);
                queue.Enqueue(next);
            }
        }

        return reachable;
    }

    private StateMachineConstruction? FindStateMachineConstruction(SyntaxNode? builderVariable, SyntaxNode buildCall, SyntaxNodeAnalysisContext context)
    {
        if (builderVariable == null)
            return null;

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

    private StateMachineConstruction? AnalyzeVariableInitializer(VariableDeclaratorSyntax declarator, SyntaxNodeAnalysisContext context)
    {
        if (declarator.Initializer?.Value is not ExpressionSyntax initializer)
            return null;

        return AnalyzeExpressionChain(initializer, context);
    }

    private StateMachineConstruction? AnalyzeAssignment(AssignmentExpressionSyntax assignment, SyntaxNodeAnalysisContext context)
    {
        return AnalyzeExpressionChain(assignment.Right, context);
    }

    private StateMachineConstruction? AnalyzeExpressionChain(ExpressionSyntax expression, SyntaxNodeAnalysisContext context)
    {
        var construction = new StateMachineConstruction();

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
                break;
            }
            else
            {
                break;
            }
        }

        return construction;
    }

    private void AnalyzeMethodCall(InvocationExpressionSyntax invocation, StateMachineConstruction construction, SyntaxNodeAnalysisContext context)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var methodName = memberAccess.Name.Identifier.ValueText;

        switch (methodName)
        {
            case "AddState":
                if (invocation.ArgumentList.Arguments.Count > 0 &&
                    invocation.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal)
                {
                    var stateName = literal.Token.ValueText;
                    construction.States.Add(stateName);
                }
                break;

            case "AddTransition":
                if (invocation.ArgumentList.Arguments.Count >= 2)
                {
                    var fromArg = invocation.ArgumentList.Arguments[0].Expression;
                    var toArg = invocation.ArgumentList.Arguments[1].Expression;

                    if (fromArg is LiteralExpressionSyntax fromLiteral &&
                        toArg is LiteralExpressionSyntax toLiteral)
                    {
                        construction.Transitions.Add((fromLiteral.Token.ValueText, toLiteral.Token.ValueText));
                    }
                }
                break;

            case "SetInitialState":
                construction.HasInitialState = true;
                if (invocation.ArgumentList.Arguments.Count > 0 &&
                    invocation.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax initialStateLiteral)
                {
                    construction.InitialState = initialStateLiteral.Token.ValueText;
                    construction.States.Add(construction.InitialState);
                }
                break;
        }
    }

    private class StateMachineConstruction
    {
        public bool HasInitialState { get; set; }
        public string InitialState { get; set; } = string.Empty;
        public HashSet<string> States { get; } = new();
        public List<(string From, string To)> Transitions { get; } = new();
    }
}
