namespace DotNetAgents.SessionPersistence.Models;

public record LessonResponse(
    Guid Id,
    Guid? ProjectId,
    string Title,
    string Category,
    DateTime CreatedAt,
    string? Message = null
);
