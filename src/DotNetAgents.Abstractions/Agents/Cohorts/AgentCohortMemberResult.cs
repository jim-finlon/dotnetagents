namespace DotNetAgents.Abstractions.Agents.Cohorts;

/// <summary>
/// Result for one member in a cohort run.
/// </summary>
public sealed record AgentCohortMemberResult
{
    /// <summary>
    /// Gets the member id.
    /// </summary>
    public required string MemberId { get; init; }

    /// <summary>
    /// Gets the member role.
    /// </summary>
    public required string Role { get; init; }

    /// <summary>
    /// Gets the runtime identity of the member instance.
    /// </summary>
    public required AgentInstanceIdentity Identity { get; init; }

    /// <summary>
    /// Gets a value indicating whether this member succeeded.
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the member output when successful.
    /// </summary>
    public string? Output { get; init; }

    /// <summary>
    /// Gets the error message when unsuccessful.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the start time.
    /// </summary>
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// Gets the completion time.
    /// </summary>
    public required DateTimeOffset CompletedAt { get; init; }

    /// <summary>
    /// Gets non-secret result metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>();
}
