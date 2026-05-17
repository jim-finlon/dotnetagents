namespace DotNetAgents.Abstractions.PublicSubstitutes.Tasks;

/// <summary>Request to record a lightweight public task.</summary>
public sealed record PublicTaskRequest(
    string Kind,
    string Description,
    IReadOnlyDictionary<string, string>? Inputs = null);
