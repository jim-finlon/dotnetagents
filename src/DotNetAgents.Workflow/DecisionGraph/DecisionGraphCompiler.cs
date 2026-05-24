// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Workflow.DecisionGraph;

/// <summary>
/// Real <see cref="IDecisionGraphCompiler"/>. Story 67a5c613. Validates the
/// definition and builds index lookups (<c>NodesById</c>, <c>OutgoingEdges</c>)
/// so the runtime can dispatch nodes + walk edges without re-scanning the
/// definition on every step. Returns a <see cref="CompiledDecisionGraph"/> as
/// the compiled artifact; the runtime-kind tag is "runtime.v1".
/// </summary>
public sealed class DecisionGraphCompiler : IDecisionGraphCompiler
{
    private readonly DecisionGraphValidator _validator;

    public DecisionGraphCompiler(DecisionGraphValidator? validator = null)
        => _validator = validator ?? new DecisionGraphValidator();

    public DecisionGraphCompilationResult Compile(DecisionGraphDefinition graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        var report = _validator.Validate(graph);
        if (!report.IsValid)
            return new DecisionGraphCompilationResult(false, "runtime.v1", null, report.Errors);

        var nodesById = graph.Nodes.ToDictionary(n => n.Id, n => n, StringComparer.Ordinal);
        var outgoing = graph.Edges
            .GroupBy(e => e.From, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<DecisionGraphEdge>)g.ToArray(), StringComparer.Ordinal);

        var compiled = new CompiledDecisionGraph
        {
            Definition = graph,
            NodesById = nodesById,
            OutgoingEdges = outgoing,
        };

        return new DecisionGraphCompilationResult(true, "runtime.v1", compiled, Array.Empty<DecisionGraphValidationError>());
    }
}
