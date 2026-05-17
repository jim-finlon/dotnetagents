namespace DotNetAgents.SessionPersistence.Models;

public record UpdateProjectRulesRequest(
    string RulesContent,
    string FormatType = "Both",
    IReadOnlyList<string>? Categories = null,
    Dictionary<string, string>? Metadata = null
);
