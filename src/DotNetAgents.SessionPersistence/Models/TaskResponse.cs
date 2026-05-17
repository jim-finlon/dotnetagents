namespace DotNetAgents.SessionPersistence.Models;

public record TaskResponse(
    Guid Id,
    Guid ProjectId,
    string Content,
    string Status,
    string Priority,
    int Order,
    IReadOnlyList<Guid>? DependsOn,
    IReadOnlyList<string>? Tags,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? CompletedAt
);
