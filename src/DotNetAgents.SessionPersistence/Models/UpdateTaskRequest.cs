namespace DotNetAgents.SessionPersistence.Models;

public record UpdateTaskRequest(
    string? Content = null,
    string? Status = null,
    string? Priority = null,
    string? Notes = null
);
