namespace DotNetAgents.Abstractions.Intent;

/// <summary>
/// Canonical dispatch intent for Voice classification, MCP routing, and ContextIntent bridges (story 305dd821).
/// </summary>
public record AgentDispatchIntent
{
    public required string Domain { get; init; }

    public required string Action { get; init; }

    public string? SubType { get; init; }

    public Dictionary<string, object> Parameters { get; init; } = new();

    public List<string> MissingRequired { get; init; } = new();

    public double Confidence { get; init; }

    public string? TargetService { get; init; }

    public string? Tool { get; init; }

    public string? RawText { get; init; }

    public string FullName => string.IsNullOrEmpty(SubType)
        ? $"{Domain}.{Action}"
        : $"{Domain}.{Action}.{SubType}";

    public bool IsComplete => MissingRequired.Count == 0;
}
