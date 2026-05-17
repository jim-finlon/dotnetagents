namespace DotNetAgents.Abstractions.Hooks;

/// <summary>
/// Context passed to a hook at a lifecycle checkpoint. Carries the agent identity, the
/// checkpoint, the payload being intercepted, and any structured metadata the agent loop
/// has attached.
/// </summary>
/// <param name="Checkpoint">Which lifecycle checkpoint fired.</param>
/// <param name="ActorId">Stable id of the agent (or operator) acting.</param>
/// <param name="TaskId">Optional task/story id this lifecycle event is associated with.</param>
/// <param name="Payload">The payload being intercepted — tool input/output, LLM messages, error, etc. Type varies by checkpoint.</param>
/// <param name="Metadata">Free-form metadata attached by the agent loop (cost estimates, posture, story links, etc.).</param>
/// <param name="OccurredAtUtc">When the checkpoint fired.</param>
public sealed record AgentHookContext(
    HookCheckpoint Checkpoint,
    string ActorId,
    string? TaskId,
    object? Payload,
    IReadOnlyDictionary<string, object>? Metadata,
    DateTimeOffset OccurredAtUtc)
{
    /// <summary>Convenience: typed payload accessor with safe coercion.</summary>
    public T? PayloadAs<T>() where T : class => Payload as T;

    /// <summary>Convenience: typed metadata-value accessor.</summary>
    public T? GetMetadataValue<T>(string key, T? defaultValue = default)
    {
        if (Metadata is null) return defaultValue;
        if (!Metadata.TryGetValue(key, out var value)) return defaultValue;
        if (value is T typed) return typed;
        try { return (T)Convert.ChangeType(value, typeof(T))!; }
        catch { return defaultValue; }
    }
}
