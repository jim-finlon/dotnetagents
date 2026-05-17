namespace DotNetAgents.ModelRouting;

/// <summary>Tracks cost per model and total; used for budget constraints. MR-3.4.</summary>
public interface ICostTracker
{
    void Record(string modelId, decimal cost);
    decimal GetTotalCost();
    IReadOnlyDictionary<string, decimal> GetCostByModel();
    void Reset();
}
