// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;

namespace DotNetAgents.Observability.Metrics;

/// <summary>
/// In-memory implementation of <see cref="IMetricsCollector"/> for testing and development.
/// </summary>
public class InMemoryMetricsCollector : IMetricsCollector
{
    private readonly ConcurrentDictionary<string, List<MetricRecord>> _metrics = new();

    /// <inheritdoc/>
    public void RecordLatency(string operationName, TimeSpan duration, Dictionary<string, object>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(operationName))
            throw new ArgumentException("Operation name cannot be null or whitespace.", nameof(operationName));

        RecordMetric("latency", operationName, duration.TotalMilliseconds, tags);
    }

    /// <inheritdoc/>
    public void IncrementCounter(string counterName, long value = 1, Dictionary<string, object>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(counterName))
            throw new ArgumentException("Counter name cannot be null or whitespace.", nameof(counterName));

        RecordMetric("counter", counterName, value, tags);
    }

    /// <inheritdoc/>
    public void RecordGauge(string gaugeName, double value, Dictionary<string, object>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(gaugeName))
            throw new ArgumentException("Gauge name cannot be null or whitespace.", nameof(gaugeName));

        RecordMetric("gauge", gaugeName, value, tags);
    }

    /// <inheritdoc/>
    public void RecordHistogram(string histogramName, double value, Dictionary<string, object>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(histogramName))
            throw new ArgumentException("Histogram name cannot be null or whitespace.", nameof(histogramName));

        RecordMetric("histogram", histogramName, value, tags);
    }

    /// <summary>
    /// Gets all recorded metrics.
    /// </summary>
    /// <returns>A dictionary of metric types to their records.</returns>
    public IReadOnlyDictionary<string, List<MetricRecord>> GetAllMetrics()
    {
        return _metrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList());
    }

    /// <summary>
    /// Clears all recorded metrics.
    /// </summary>
    public void Clear()
    {
        _metrics.Clear();
    }

    private void RecordMetric(string type, string name, double value, Dictionary<string, object>? tags)
    {
        var key = $"{type}:{name}";
        var record = new MetricRecord
        {
            Type = type,
            Name = name,
            Value = value,
            Tags = tags ?? new Dictionary<string, object>(),
            Timestamp = DateTime.UtcNow
        };

        _metrics.AddOrUpdate(
            key,
            new List<MetricRecord> { record },
            (_, list) =>
            {
                list.Add(record);
                return list;
            });
    }

    /// <summary>
    /// Represents a metric record.
    /// </summary>
    public record MetricRecord
    {
        public string Type { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public double Value { get; init; }
        public Dictionary<string, object> Tags { get; init; } = new();
        public DateTime Timestamp { get; init; }
    }
}
