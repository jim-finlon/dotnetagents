namespace DotNetAgents.Agents.Tasks;

/// <summary>
/// Result of a worker task execution.
/// </summary>
public record WorkerTaskResult
{
    /// <summary>
    /// Gets the ID of the task that was executed.
    /// </summary>
    public string TaskId { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the task execution was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the output data from the task execution (if successful).
    /// </summary>
    public object? Output { get; init; }

    /// <summary>
    /// Gets the error message if the task execution failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the ID of the worker agent that executed the task.
    /// </summary>
    public string WorkerAgentId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the execution time of the task.
    /// </summary>
    public TimeSpan ExecutionTime { get; init; }

    /// <summary>
    /// Gets additional metadata associated with the result.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Gets the timestamp when the task completed.
    /// </summary>
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;
}
