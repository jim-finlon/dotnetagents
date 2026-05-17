namespace DotNetAgents.Mcp.Models;

public sealed class AgentLearningProjectionOptions
{
    public bool Enabled { get; set; } = true;
    public int TimeoutMs { get; set; } = 1500;
    public List<AgentLearningProjectionTarget> Targets { get; set; } = [];
}

public sealed class AgentLearningProjectionTarget
{
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string Url { get; set; } = string.Empty;
    public string? ApiKeyHeader { get; set; } = "X-Api-Key";
    public string? ApiKey { get; set; }
}

public sealed record AgentLearningProjectionResult(
    int AttemptedTargets,
    int SuccessfulTargets,
    IReadOnlyList<string> FailedTargets);
