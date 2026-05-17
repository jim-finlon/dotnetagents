namespace DotNetAgents.Abstractions.PublicSubstitutes.Tasks;

/// <summary>Handle returned when a public task starts.</summary>
public sealed record PublicTaskHandle(string Id, DateTimeOffset StartedAt);
