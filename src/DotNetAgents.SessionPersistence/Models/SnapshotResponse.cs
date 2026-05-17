namespace DotNetAgents.SessionPersistence.Models;

public record SnapshotResponse(
    Guid Id,
    Guid ProjectId,
    int SnapshotNumber,
    string? ResumePoint,
    int LessonCount,
    string Trigger,
    DateTime CreatedAt
);
