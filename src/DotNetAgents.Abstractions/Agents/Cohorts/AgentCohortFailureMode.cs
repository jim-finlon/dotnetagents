// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Abstractions.Agents.Cohorts;

/// <summary>
/// Determines how a cohort runner handles individual member failures.
/// </summary>
public enum AgentCohortFailureMode
{
    /// <summary>
    /// Continue running remaining members and include the failed member in the result bundle.
    /// </summary>
    ContinueOnMemberFailure = 0,

    /// <summary>
    /// Stop the cohort run when the first member fails.
    /// </summary>
    StopOnFirstFailure = 1
}
