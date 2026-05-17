namespace DotNetAgents.A2A;

/// <summary>Streaming event during task execution. FR-A2A-002.</summary>
public sealed record A2AEvent
{
    public string TaskId { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public object? Payload { get; init; }
}
