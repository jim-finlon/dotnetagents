// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Governance.AgentLifecycle;

/// <summary>
/// Lifecycle state of an AgentDefinition in the verified-publishing flow.
/// Non-admin invocations must be restricted to <see cref="Published"/>.
/// </summary>
public enum AgentLifecycleState
{
    Draft = 0,
    InReview = 1,
    Published = 2,
    Deprecated = 3,
    Retired = 4
}
