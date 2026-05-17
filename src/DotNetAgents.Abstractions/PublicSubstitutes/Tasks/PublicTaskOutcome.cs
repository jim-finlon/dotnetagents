namespace DotNetAgents.Abstractions.PublicSubstitutes.Tasks;

/// <summary>Flattened outcome for a public task.</summary>
public sealed record PublicTaskOutcome(
    bool Success,
    string? Summary = null,
    IReadOnlyDictionary<string, string>? Outputs = null);
