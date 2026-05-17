namespace DotNetAgents.Observability.Metrics;

/// <summary>
/// Interface for tracking costs associated with LLM calls and workflow executions.
/// </summary>
public interface ICostTracker
{
    /// <summary>
    /// Records an LLM call with token usage.
    /// </summary>
    /// <param name="model">The model name used for the call.</param>
    /// <param name="inputTokens">The number of input tokens.</param>
    /// <param name="outputTokens">The number of output tokens.</param>
    /// <param name="correlationId">Optional correlation ID for tracking.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RecordLLMCallAsync(
        string model,
        int inputTokens,
        int outputTokens,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a cost summary for the specified time period.
    /// </summary>
    /// <param name="period">The time period to summarize.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A cost summary for the period.</returns>
    Task<CostSummary> GetCostSummaryAsync(
        TimeSpan period,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Estimates the cost for a given model and token count.
    /// </summary>
    /// <param name="model">The model name.</param>
    /// <param name="estimatedInputTokens">The estimated number of input tokens.</param>
    /// <param name="estimatedOutputTokens">The estimated number of output tokens.</param>
    /// <returns>The estimated cost in USD.</returns>
    decimal EstimateCost(
        string model,
        int estimatedInputTokens,
        int estimatedOutputTokens);

    /// <summary>
    /// Gets the cost breakdown by model for the specified time period.
    /// </summary>
    /// <param name="period">The time period to analyze.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A dictionary mapping model names to their costs.</returns>
    Task<Dictionary<string, decimal>> GetCostByModelAsync(
        TimeSpan period,
        CancellationToken cancellationToken = default);
}
