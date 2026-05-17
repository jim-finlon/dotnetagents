namespace DotNetAgents.A2A;

/// <summary>Skill exposed by an A2A agent. FR-A2A-001.</summary>
public sealed record Skill
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    /// <summary>Input schema (e.g. JSON schema object).</summary>
    public object? InputSchema { get; init; }
    /// <summary>Output schema (e.g. JSON schema object).</summary>
    public object? OutputSchema { get; init; }
}
