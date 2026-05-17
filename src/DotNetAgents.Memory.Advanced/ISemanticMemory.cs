namespace DotNetAgents.Memory.Advanced;

/// <summary>Semantic memory: facts (triples), query, contradiction check. FR-MEM-002.</summary>
public interface ISemanticMemory
{
    Task StoreFactAsync(Fact fact, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Fact>> QueryAsync(string query, int limit = 50, CancellationToken cancellationToken = default);
    Task UpdateBeliefAsync(string factId, double confidence, CancellationToken cancellationToken = default);
    Task<bool> ContradictionCheckAsync(Fact newFact, CancellationToken cancellationToken = default);
}
