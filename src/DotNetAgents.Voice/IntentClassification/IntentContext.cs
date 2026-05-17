namespace DotNetAgents.Voice.IntentClassification;

/// <summary>
/// Represents the context in which an intent is being classified.
/// </summary>
public record IntentContext
{
    /// <summary>
    /// Gets the current activity ID (if user is viewing/working on an activity).
    /// </summary>
    public Guid? CurrentActivityId { get; init; }

    /// <summary>
    /// Gets the current goal ID (if user is viewing/working on a goal).
    /// </summary>
    public Guid? CurrentGoalId { get; init; }

    /// <summary>
    /// Gets the current plan ID (if user is viewing/working on a daily plan).
    /// </summary>
    public Guid? CurrentPlanId { get; init; }

    /// <summary>
    /// Gets the current time context.
    /// </summary>
    public DateTime CurrentTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets additional context data as key-value pairs.
    /// </summary>
    public Dictionary<string, object> AdditionalContext { get; init; } = new();

    /// <summary>
    /// Stable facts and preferences the assistant should remember for this user (from long-term store).
    /// </summary>
    public string? LongTermUserMemory { get; init; }

    /// <summary>
    /// Gets a value indicating whether this context is empty (no contextual information).
    /// </summary>
    public bool IsEmpty => CurrentActivityId == null
        && CurrentGoalId == null
        && CurrentPlanId == null
        && AdditionalContext.Count == 0
        && string.IsNullOrWhiteSpace(LongTermUserMemory);
}
