namespace DotNetAgents.A2A;

/// <summary>Response to an A2A task. FR-A2A-002.</summary>
public sealed record A2AResponse
{
    public string TaskId { get; init; } = string.Empty;
    public bool Success { get; init; }
    public object? Output { get; init; }
    public string? Error { get; init; }
}
