namespace DotNetAgents.Abstractions.Agents.Cohorts;

/// <summary>
/// Defines a coordinated run of multiple agent instances against one shared task.
/// </summary>
/// <typeparam name="TConfiguration">The caller-defined configuration snapshot type.</typeparam>
public sealed record AgentCohortDefinition<TConfiguration>
{
    /// <summary>
    /// Gets the stable cohort id.
    /// </summary>
    public required string CohortId { get; init; }

    /// <summary>
    /// Gets the display name for dashboards and reports.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets the shared task for all cohort members.
    /// </summary>
    public required AgentCohortSharedTask SharedTask { get; init; }

    /// <summary>
    /// Gets the cohort members.
    /// </summary>
    public required IReadOnlyList<AgentCohortMember<TConfiguration>> Members { get; init; }

    /// <summary>
    /// Gets run-level correlation metadata.
    /// </summary>
    public AgentRuntimeCorrelation Correlation { get; init; } = new();

    /// <summary>
    /// Gets isolation and failure behavior expectations.
    /// </summary>
    public AgentCohortIsolationPolicy IsolationPolicy { get; init; } = new();

    /// <summary>
    /// Gets a named aggregation policy for consumers that score or summarize results.
    /// </summary>
    public string ResultAggregationPolicy { get; init; } = "collect-all";

    /// <summary>
    /// Gets the time this definition was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Validates the cohort definition.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(CohortId))
        {
            throw new ArgumentException("Cohort id is required.", nameof(CohortId));
        }

        ArgumentNullException.ThrowIfNull(SharedTask);
        SharedTask.Validate();

        if (Members is null || Members.Count == 0)
        {
            throw new ArgumentException("Cohort must contain at least one member.", nameof(Members));
        }

        var memberIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var instanceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var member in Members)
        {
            ArgumentNullException.ThrowIfNull(member);
            member.Validate();

            if (!memberIds.Add(member.MemberId))
            {
                throw new ArgumentException(
                    $"Duplicate cohort member id '{member.MemberId}'.",
                    nameof(Members));
            }

            var instanceId = member.InstanceRequest.Identity.InstanceId;
            if (!instanceIds.Add(instanceId))
            {
                throw new ArgumentException(
                    $"Duplicate agent instance id '{instanceId}'.",
                    nameof(Members));
            }
        }
    }
}
