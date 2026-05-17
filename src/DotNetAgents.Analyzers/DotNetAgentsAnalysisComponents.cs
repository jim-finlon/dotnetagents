using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DotNetAgents.Analyzers;

/// <summary>
/// Registration surface for hosts, tooling, and tests: enumerates Roslyn analyzers and source generators
/// shipped in this assembly without relying on reflection over the whole assembly.
/// </summary>
public static class DotNetAgentsAnalysisComponents
{
    /// <summary>Diagnostic analyzers (compile-time validation) exposed by DotNetAgents.Analyzers.</summary>
    public static ImmutableArray<Type> DiagnosticAnalyzers { get; } = ImmutableArray.Create(
        typeof(WorkflowAnalyzer),
        typeof(StateMachineAnalyzer));

    /// <summary>Source generators exposed by DotNetAgents.Analyzers.</summary>
    public static ImmutableArray<Type> SourceGenerators { get; } = ImmutableArray.Create(
        typeof(WorkflowSourceGenerator));

    /// <summary>Returns <see cref="DiagnosticAnalyzers"/> types that derive from <typeparamref name="T"/>.</summary>
    public static ImmutableArray<Type> OfAnalyzer<T>() where T : DiagnosticAnalyzer =>
        DiagnosticAnalyzers.Where(t => typeof(T).IsAssignableFrom(t)).ToImmutableArray();
}
