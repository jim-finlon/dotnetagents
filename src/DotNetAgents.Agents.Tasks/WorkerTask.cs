// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Agents.Tasks;

/// <summary>
/// Represents a task to be executed by a worker agent.
/// </summary>
public record WorkerTask
{
    /// <summary>
    /// Gets the unique identifier of the task.
    /// </summary>
    public string TaskId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets the type of the task (e.g., "analyze_document", "process_data").
    /// </summary>
    public string TaskType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the input data for the task.
    /// </summary>
    public object Input { get; init; } = new();

    /// <summary>
    /// Gets the required capability for executing this task (optional).
    /// </summary>
    public string? RequiredCapability { get; init; }

    /// <summary>
    /// Gets the preferred agent ID for executing this task (optional).
    /// </summary>
    public string? PreferredAgentId { get; init; }

    /// <summary>
    /// Gets the priority of the task (higher numbers = higher priority).
    /// </summary>
    public int Priority { get; init; } = 0;

    /// <summary>
    /// Gets the timeout for task execution (optional).
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Gets the queue key used by control-loop dashboards to group pending work.
    /// </summary>
    public string? QueueKey { get; init; }

    /// <summary>
    /// Gets the correlation id that links this work item to traces, stories, or loop runs.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets the SDLC story id associated with the work, when one exists.
    /// </summary>
    public string? StoryId { get; init; }

    /// <summary>
    /// Gets the parent work item id for fan-out/fan-in lineage.
    /// </summary>
    public string? ParentTaskId { get; init; }

    /// <summary>
    /// Gets the current retry count for this task.
    /// </summary>
    public int RetryCount { get; init; }

    /// <summary>
    /// Gets the maximum attempts allowed before the task should be treated as failed or quarantined.
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>
    /// Gets the earliest time the task should be retried, if it is waiting after a failure.
    /// </summary>
    public DateTimeOffset? NextRetryAtUtc { get; init; }

    /// <summary>
    /// Gets additional metadata associated with the task.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Gets the timestamp when the task was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
