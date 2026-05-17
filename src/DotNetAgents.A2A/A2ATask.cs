namespace DotNetAgents.A2A;

/// <summary>Incoming A2A task. FR-A2A-002.</summary>
public sealed record A2ATask
{
    public string Id { get; init; } = string.Empty;
    public string Skill { get; init; } = string.Empty;
    /// <summary>Task input (e.g. JSON object).</summary>
    public object? Input { get; init; }
    public A2AMetadata? Metadata { get; init; }
}

/// <summary>Task metadata (e.g. correlation id).</summary>
public sealed record A2AMetadata
{
    public string? CorrelationId { get; init; }
    public IReadOnlyDictionary<string, string>? Headers { get; init; }
}
