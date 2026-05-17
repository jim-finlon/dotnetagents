namespace DotNetAgents.SessionPersistence.Models;

public record ListSessionsResponse(
    IReadOnlyList<SessionSummary> Sessions,
    int TotalCount
);

public record SessionSummary(
    Guid Id,
    string Name,
    string? Description,
    string Status,
    DateTime UpdatedAt,
    TaskStatsSummary? TaskStats
);

public record TaskStatsSummary(int Total, int Completed);
