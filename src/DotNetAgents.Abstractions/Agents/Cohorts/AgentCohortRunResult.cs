namespace DotNetAgents.Abstractions.Agents.Cohorts;

/// <summary>
/// Evidence bundle for a completed cohort run.
/// </summary>
public sealed record AgentCohortRunResult
{
    /// <summary>
    /// Gets the stable schema version for serialized cohort run evidence.
    /// </summary>
    public string SchemaVersion { get; init; } = "dna.agent.cohort.run.v1";

    /// <summary>
    /// Gets the cohort id.
    /// </summary>
    public required string CohortId { get; init; }

    /// <summary>
    /// Gets the run id for this execution.
    /// </summary>
    public required string RunId { get; init; }

    /// <summary>
    /// Gets run-level correlation metadata.
    /// </summary>
    public AgentRuntimeCorrelation Correlation { get; init; } = new();

    /// <summary>
    /// Gets the aggregation policy requested by the definition.
    /// </summary>
    public required string ResultAggregationPolicy { get; init; }

    /// <summary>
    /// Gets a value indicating whether every executed member succeeded.
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the number of successful member results.
    /// </summary>
    public int SucceededMemberCount { get; init; }

    /// <summary>
    /// Gets the number of failed member results.
    /// </summary>
    public int FailedMemberCount { get; init; }

    /// <summary>
    /// Gets the start time.
    /// </summary>
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// Gets the completion time.
    /// </summary>
    public required DateTimeOffset CompletedAt { get; init; }

    /// <summary>
    /// Gets all member results that were executed.
    /// </summary>
    public required IReadOnlyList<AgentCohortMemberResult> MemberResults { get; init; }
}
