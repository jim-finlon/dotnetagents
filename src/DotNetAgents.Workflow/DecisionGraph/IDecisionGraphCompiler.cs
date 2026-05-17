namespace DotNetAgents.Workflow.DecisionGraph;

/// <summary>
/// Compiler contract that maps a validated <see cref="DecisionGraphDefinition"/>
/// into a runtime-executable representation. Story 55db0c7d.
///
/// The actual mapping into <see cref="StateGraph{TState}"/> / behavior-tree /
/// workflow primitives is the runtime executor story (67a5c613). This interface
/// exists so the validator + operator API + MCP tools can take a dependency
/// today and the runtime impl can land later without changing call sites.
/// </summary>
public interface IDecisionGraphCompiler
{
    /// <summary>Compile a definition. Returns the compiled artifact + the runtime kind tag stored in <c>jarvis_decision_graph_versions.CompiledRuntimeKind</c>.</summary>
    DecisionGraphCompilationResult Compile(DecisionGraphDefinition graph);
}

public sealed record DecisionGraphCompilationResult(
    bool Succeeded,
    string CompiledRuntimeKind,
    object? CompiledArtifact,
    IReadOnlyList<DecisionGraphValidationError> Errors);

/// <summary>
/// Default compiler used until the runtime executor lands. Returns
/// <see cref="DecisionGraphCompilationResult.Succeeded"/>=false with a single
/// "compiler.notImplemented" error and a known runtime-kind tag so persistence
/// callers can still record the version row + ValidationReportJson.
/// </summary>
public sealed class NoOpDecisionGraphCompiler : IDecisionGraphCompiler
{
    public DecisionGraphCompilationResult Compile(DecisionGraphDefinition graph) =>
        new(false, "noop", null, new[] { new DecisionGraphValidationError("compiler.notImplemented", "Runtime compiler is not yet wired (story 67a5c613). Validation passed; compilation deferred.") });
}
