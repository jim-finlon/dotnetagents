namespace DotNetAgents.Mcp.Server;

/// <summary>
/// Configuration for projecting MCP tool-call outcomes into lesson.event.v1.
/// </summary>
public sealed class McpLearningDecoratorOptions
{
    public string Service { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string ActorType { get; set; } = "agent";
    public string ActorId { get; set; } = string.Empty;
    public string EventLogPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "data", "learning-events.ndjson");
    public double SuccessConfidence { get; set; } = 0.75;
    public double FailureConfidence { get; set; } = 0.5;
}
