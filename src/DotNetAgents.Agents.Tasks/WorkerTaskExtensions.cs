namespace DotNetAgents.Agents.Tasks;

/// <summary>
/// Extension methods for creating <see cref="WorkerTask"/> from <see cref="AgentHandoff"/>.
/// Keeps handoff-to-task conversion in one place for consistent TaskType, Input, Timeout, and Metadata.
/// </summary>
public static class WorkerTaskExtensions
{
    /// <summary>
    /// Creates a <see cref="WorkerTask"/> from an <see cref="AgentHandoff"/>.
    /// Sets TaskType, Input (the handoff), Timeout from Constraints, and optional Metadata.
    /// </summary>
    /// <param name="handoff">The handoff to convert.</param>
    /// <param name="taskType">Task type for routing (e.g. "developmental-editor", "line-copy-editor").</param>
    /// <param name="priority">Task priority (default 0).</param>
    /// <param name="preferredAgentId">Optional preferred agent ID.</param>
    /// <param name="requiredCapability">Optional required capability for routing.</param>
    /// <param name="metadata">Optional extra metadata to merge (handoff.References are not auto-copied).</param>
    /// <returns>A WorkerTask suitable for submission to a supervisor (e.g. ISupervisorAgent.SubmitTaskAsync).</returns>
    public static WorkerTask ToWorkerTask(
        this AgentHandoff handoff,
        string taskType,
        int priority = 0,
        string? preferredAgentId = null,
        string? requiredCapability = null,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(handoff);
        if (string.IsNullOrWhiteSpace(taskType))
            throw new ArgumentException("Task type is required.", nameof(taskType));

        var timeout = handoff.Constraints?.Timeout;
        var meta = new Dictionary<string, object>();
        if (metadata != null)
        {
            foreach (var kv in metadata)
                meta[kv.Key] = kv.Value;
        }
        if (handoff.Constraints?.MaxTokenBudget is int budget)
            meta["MaxTokenBudget"] = budget;
        if (handoff.Constraints?.MaxIterations is int maxIter)
            meta["MaxIterations"] = maxIter;

        return new WorkerTask
        {
            TaskType = taskType,
            Input = handoff,
            Timeout = timeout,
            Priority = priority,
            PreferredAgentId = preferredAgentId,
            RequiredCapability = requiredCapability,
            Metadata = meta
        };
    }
}
