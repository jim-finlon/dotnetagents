namespace DotNetAgents.SessionPersistence.Models;

public record ProjectRulesResponse(
    Guid Id,
    Guid ProjectId,
    string RulesContent,
    string FormatType,
    IReadOnlyList<string>? Categories,
    Dictionary<string, string>? Metadata,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
