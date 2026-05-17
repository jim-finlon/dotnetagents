namespace DotNetAgents.Memory.Advanced;

/// <summary>Knowledge triple (subject, predicate, object). FR-MEM-002.</summary>
public sealed record Fact
{
    public string Id { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Predicate { get; init; } = string.Empty;
    public string Object { get; init; } = string.Empty;
    public double Confidence { get; init; } = 1.0;
    public IReadOnlyList<string> Sources { get; init; } = Array.Empty<string>();
}
