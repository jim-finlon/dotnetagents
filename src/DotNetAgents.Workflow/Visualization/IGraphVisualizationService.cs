using DotNetAgents.Workflow.Graph;

namespace DotNetAgents.Workflow.Visualization;

/// <summary>
/// Interface for visualizing workflow graphs.
/// </summary>
public interface IGraphVisualizationService
{
    /// <summary>
    /// Generates a Graphviz DOT format string for the workflow graph.
    /// </summary>
    /// <typeparam name="TState">The type of the workflow state.</typeparam>
    /// <param name="graph">The workflow graph to visualize.</param>
    /// <returns>A DOT format string.</returns>
    string GenerateDot<TState>(StateGraph<TState> graph) where TState : class;

    /// <summary>
    /// Generates a Mermaid diagram string for the workflow graph.
    /// </summary>
    /// <typeparam name="TState">The type of the workflow state.</typeparam>
    /// <param name="graph">The workflow graph to visualize.</param>
    /// <returns>A Mermaid diagram string.</returns>
    string GenerateMermaid<TState>(StateGraph<TState> graph) where TState : class;

    /// <summary>
    /// Generates JSON metadata for the workflow graph.
    /// </summary>
    /// <typeparam name="TState">The type of the workflow state.</typeparam>
    /// <param name="graph">The workflow graph to serialize.</param>
    /// <returns>A JSON string containing graph metadata.</returns>
    string GenerateJson<TState>(StateGraph<TState> graph) where TState : class;
}
