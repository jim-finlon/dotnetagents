namespace DotNetAgents.AgenticRAG;

/// <summary>
/// Adaptive RAG: selects strategy from query analysis and delegates to registered strategies.
/// FR-RAG-003.
/// </summary>
public sealed class AdaptiveRAG : IAdaptiveRAG
{
    private readonly Func<string, CancellationToken, Task<QueryAnalysis>> _analyzeQuery;
    private readonly Dictionary<string, IRetrievalStrategy> _strategies = new();

    public AdaptiveRAG(Func<string, CancellationToken, Task<QueryAnalysis>>? analyzeQuery = null)
    {
        _analyzeQuery = analyzeQuery ?? ((q, ct) => Task.FromResult(new QueryAnalysis { Complexity = QueryComplexity.Moderate, SuggestedStrategy = "direct" }));
    }

    /// <inheritdoc />
    public Task<QueryAnalysis> SelectStrategyAsync(string query, CancellationToken cancellationToken = default)
        => _analyzeQuery(query, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RetrievedDocument>> RetrieveAsync(
        string query,
        IRetrievalStrategy? strategy = null,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        var toUse = strategy;
        if (toUse == null)
        {
            var analysis = await SelectStrategyAsync(query, cancellationToken).ConfigureAwait(false);
            var name = analysis.SuggestedStrategy ?? "direct";
            toUse = GetStrategy(name) ?? throw new InvalidOperationException($"No strategy registered for '{name}'.");
        }
        return await toUse.RetrieveAsync(query, topK, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void RegisterStrategy(IRetrievalStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(strategy);
        _strategies[strategy.Name] = strategy;
    }

    /// <inheritdoc />
    public IRetrievalStrategy? GetStrategy(string name) => _strategies.TryGetValue(name, out var s) ? s : null;
}
