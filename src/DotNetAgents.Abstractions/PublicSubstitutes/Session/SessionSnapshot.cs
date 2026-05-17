namespace DotNetAgents.Abstractions.PublicSubstitutes.Session;

/// <summary>Small public session state payload for local examples and adapters.</summary>
public sealed record SessionSnapshot(
    SessionId Id,
    string SchemaVersion,
    IReadOnlyDictionary<string, string> Properties,
    DateTimeOffset UpdatedAt);
