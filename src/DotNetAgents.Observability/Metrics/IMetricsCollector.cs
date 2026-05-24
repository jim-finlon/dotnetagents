// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Observability.Metrics;

/// <summary>
/// Interface for collecting performance metrics.
/// </summary>
public interface IMetricsCollector
{
    /// <summary>
    /// Records the latency of an operation.
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="duration">The duration of the operation.</param>
    /// <param name="tags">Optional tags for categorization.</param>
    void RecordLatency(string operationName, TimeSpan duration, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Increments a counter.
    /// </summary>
    /// <param name="counterName">The name of the counter.</param>
    /// <param name="value">The value to increment by (default is 1).</param>
    /// <param name="tags">Optional tags for categorization.</param>
    void IncrementCounter(string counterName, long value = 1, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Records a gauge value.
    /// </summary>
    /// <param name="gaugeName">The name of the gauge.</param>
    /// <param name="value">The gauge value.</param>
    /// <param name="tags">Optional tags for categorization.</param>
    void RecordGauge(string gaugeName, double value, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Records a histogram value.
    /// </summary>
    /// <param name="histogramName">The name of the histogram.</param>
    /// <param name="value">The histogram value.</param>
    /// <param name="tags">Optional tags for categorization.</param>
    void RecordHistogram(string histogramName, double value, Dictionary<string, object>? tags = null);
}
