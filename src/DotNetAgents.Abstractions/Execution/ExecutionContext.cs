using System.Diagnostics;

namespace DotNetAgents.Abstractions.Execution;

/// <summary>
/// Provides execution context for chains, workflows, and agents.
/// </summary>
public class ExecutionContext
{
    /// <summary>
    /// Gets or sets the correlation ID for distributed tracing.
    /// </summary>
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets additional metadata associated with this execution.
    /// </summary>
    public IDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets the cancellation token for this execution.
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// Gets or sets the logger for this execution context.
    /// Note: This is stored as object to avoid dependency on Microsoft.Extensions.Logging in Abstractions.
    /// </summary>
    public object? Logger { get; set; }

    /// <summary>
    /// Gets or sets the OpenTelemetry activity for distributed tracing.
    /// </summary>
    public Activity? Activity { get; set; }

    /// <summary>
    /// Gets or sets the start time of the execution.
    /// </summary>
    public DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Creates a new execution context with default values.
    /// </summary>
    /// <returns>A new execution context instance.</returns>
    public static ExecutionContext Create() => new();

    /// <summary>
    /// Creates a new execution context with a specific cancellation token.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to use.</param>
    /// <returns>A new execution context instance.</returns>
    public static ExecutionContext Create(CancellationToken cancellationToken) =>
        new() { CancellationToken = cancellationToken };
}

/// <summary>
/// Provides access to the current execution context.
/// </summary>
public interface IExecutionContextProvider
{
    /// <summary>
    /// Gets the current execution context.
    /// </summary>
    /// <returns>The current execution context, or null if none is set.</returns>
    ExecutionContext? GetCurrent();

    /// <summary>
    /// Sets the current execution context.
    /// </summary>
    /// <param name="context">The execution context to set.</param>
    void SetCurrent(ExecutionContext? context);
}
