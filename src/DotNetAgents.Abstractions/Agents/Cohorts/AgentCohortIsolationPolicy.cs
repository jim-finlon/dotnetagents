// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Abstractions.Agents.Cohorts;

/// <summary>
/// Describes isolation expectations for members in one cohort run.
/// </summary>
public sealed record AgentCohortIsolationPolicy
{
    /// <summary>
    /// Gets a value indicating whether each member must receive its own configuration snapshot.
    /// </summary>
    public bool IsolateConfiguration { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether each member should avoid shared mutable memory by default.
    /// </summary>
    public bool IsolateMemory { get; init; } = true;

    /// <summary>
    /// Gets the failure behavior for the cohort.
    /// </summary>
    public AgentCohortFailureMode FailureMode { get; init; } =
        AgentCohortFailureMode.ContinueOnMemberFailure;
}
