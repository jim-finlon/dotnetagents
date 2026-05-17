using System.Collections.Concurrent;

namespace DotNetAgents.Observability.Metrics;

/// <summary>
/// In-memory implementation of <see cref="ICostTracker"/> for testing and development.
/// </summary>
public class InMemoryCostTracker : ICostTracker
{
    private readonly ConcurrentBag<CostRecord> _records = new();

    /// <inheritdoc/>
    public Task RecordLLMCallAsync(
        string model,
        int inputTokens,
        int outputTokens,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Model name cannot be null or whitespace.", nameof(model));

        if (inputTokens < 0)
            throw new ArgumentException("Input tokens cannot be negative.", nameof(inputTokens));

        if (outputTokens < 0)
            throw new ArgumentException("Output tokens cannot be negative.", nameof(outputTokens));

        var cost = ModelPricing.CalculateCost(model, inputTokens, outputTokens) ?? 0m;
        var record = new CostRecord
        {
            Model = model,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            Cost = cost,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow
        };

        _records.Add(record);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<CostSummary> GetCostSummaryAsync(
        TimeSpan period,
        CancellationToken cancellationToken = default)
    {
        var endTime = DateTime.UtcNow;
        var startTime = endTime - period;

        var relevantRecords = _records
            .Where(r => r.Timestamp >= startTime && r.Timestamp <= endTime)
            .ToList();

        var totalCost = relevantRecords.Sum(r => r.Cost);
        var totalCalls = relevantRecords.Count;
        var totalInputTokens = relevantRecords.Sum(r => (long)r.InputTokens);
        var totalOutputTokens = relevantRecords.Sum(r => (long)r.OutputTokens);

        var costByModel = relevantRecords
            .GroupBy(r => r.Model)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Cost));

        var summary = new CostSummary
        {
            TotalCost = totalCost,
            TotalCalls = totalCalls,
            TotalInputTokens = totalInputTokens,
            TotalOutputTokens = totalOutputTokens,
            Period = period,
            StartTime = startTime,
            EndTime = endTime,
            CostByModel = costByModel
        };

        return Task.FromResult(summary);
    }

    /// <inheritdoc/>
    public decimal EstimateCost(
        string model,
        int estimatedInputTokens,
        int estimatedOutputTokens)
    {
        return ModelPricing.CalculateCost(model, estimatedInputTokens, estimatedOutputTokens) ?? 0m;
    }

    /// <inheritdoc/>
    public Task<Dictionary<string, decimal>> GetCostByModelAsync(
        TimeSpan period,
        CancellationToken cancellationToken = default)
    {
        var summary = GetCostSummaryAsync(period, cancellationToken).Result;
        return Task.FromResult(new Dictionary<string, decimal>(summary.CostByModel));
    }

    private record CostRecord
    {
        public string Model { get; init; } = string.Empty;
        public int InputTokens { get; init; }
        public int OutputTokens { get; init; }
        public decimal Cost { get; init; }
        public string? CorrelationId { get; init; }
        public DateTime Timestamp { get; init; }
    }
}
