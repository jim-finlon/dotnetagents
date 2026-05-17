namespace DotNetAgents.SessionPersistence.Models;

public record QueryLessonsResponse(
    IReadOnlyList<LessonInfo> Lessons,
    int TotalCount
);
