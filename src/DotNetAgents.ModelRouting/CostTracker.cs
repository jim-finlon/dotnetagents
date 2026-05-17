namespace DotNetAgents.ModelRouting;

/// <summary>In-memory cost tracker per model. MR-3.4.</summary>
public sealed class CostTracker : ICostTracker
{
    private readonly Dictionary<string, decimal> _byModel = new(StringComparer.OrdinalIgnoreCase);
    private decimal _total;
    private readonly object _lock = new();

    /// <inheritdoc />
    public void Record(string modelId, decimal cost)
    {
        if (string.IsNullOrEmpty(modelId)) return;
        lock (_lock)
        {
            _byModel.TryGetValue(modelId, out var existing);
            _byModel[modelId] = existing + cost;
            _total += cost;
        }
    }

    /// <inheritdoc />
    public decimal GetTotalCost()
    {
        lock (_lock) return _total;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, decimal> GetCostByModel()
    {
        lock (_lock)
            return new Dictionary<string, decimal>(_byModel);
    }

    /// <inheritdoc />
    public void Reset()
    {
        lock (_lock)
        {
            _byModel.Clear();
            _total = 0;
        }
    }
}
