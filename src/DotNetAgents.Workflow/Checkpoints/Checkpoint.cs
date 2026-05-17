namespace DotNetAgents.Workflow.Checkpoints;

/// <summary>
/// Represents a checkpoint of workflow state at a specific point in execution.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public record Checkpoint<TState> where TState : class
{
    /// <summary>
    /// Gets the unique identifier for this checkpoint.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets the workflow run identifier this checkpoint belongs to.
    /// </summary>
    public string RunId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the name of the node where this checkpoint was created.
    /// </summary>
    public string NodeName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the serialized state at this checkpoint.
    /// </summary>
    public string SerializedState { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when this checkpoint was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the version of the state schema.
    /// </summary>
    public int StateVersion { get; init; } = 1;

    /// <summary>
    /// Gets optional metadata associated with this checkpoint.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}
