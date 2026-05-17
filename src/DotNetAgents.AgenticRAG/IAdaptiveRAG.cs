namespace DotNetAgents.AgenticRAG;

/// <summary>
/// Adaptive RAG: analyzes query, selects strategy, retrieves. Supports custom strategy registration.
/// FR-RAG-003.
/// </summary>
public interface IAdaptiveRAG
{
    /// <summary>Analyzes the query and suggests a strategy.</summary>
    Task<QueryAnalysis> SelectStrategyAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>Retrieves using the given strategy, or auto-selected if null.</summary>
    Task<IReadOnlyList<RetrievedDocument>> RetrieveAsync(
        string query,
        IRetrievalStrategy? strategy = null,
        int topK = 5,
        CancellationToken cancellationToken = default);

    /// <summary>Registers a custom strategy by name.</summary>
    void RegisterStrategy(IRetrievalStrategy strategy);

    /// <summary>Gets a registered strategy by name.</summary>
    IRetrievalStrategy? GetStrategy(string name);
}
