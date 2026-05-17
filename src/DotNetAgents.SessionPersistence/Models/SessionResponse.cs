namespace DotNetAgents.SessionPersistence.Models;

public record SessionResponse(
    Guid Id,
    string Name,
    string? Description,
    string Status,
    string? CurrentResumePoint,
    int SnapshotCount,
    string? WorkspaceId,
    string? GitRepository,
    string? GitBranch,
    Dictionary<string, string>? Metadata,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? LastAccessedAt,
    DateTime? CompletedAt
);
