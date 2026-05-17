namespace DotNetAgents.Abstractions.PublicSubstitutes.Memory;

/// <summary>
/// Public durable-memory lesson payload for open-core agents and examples.
/// Premium/private systems can adapt this contract to KnowledgeMemory without
/// exposing private factory storage details.
/// </summary>
public sealed record PublicLessonRecord(
    LessonId Id,
    string Namespace,
    string Title,
    string Body,
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, string> Metadata,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
