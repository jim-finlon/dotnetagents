namespace DotNetAgents.SessionPersistence.Models;

public record UpdateSessionRequest(
    string? CurrentResumePoint = null,
    string? Status = null,
    Dictionary<string, string>? Metadata = null,
    string? ResumePoint = null
);
