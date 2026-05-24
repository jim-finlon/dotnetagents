// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Governance.AgentLifecycle;

/// <summary>
/// Guard for allowed lifecycle transitions. Services persisting an
/// <see cref="AgentLifecycleState"/> call <see cref="CanTransition"/> before applying a change
/// and <see cref="Assert"/> when they want to throw on illegal moves.
/// </summary>
public static class AgentLifecycleTransition
{
    public static bool CanTransition(AgentLifecycleState from, AgentLifecycleState to) =>
        (from, to) switch
        {
            (AgentLifecycleState.Draft, AgentLifecycleState.InReview) => true,
            (AgentLifecycleState.Draft, AgentLifecycleState.Retired) => true,
            (AgentLifecycleState.InReview, AgentLifecycleState.Draft) => true,
            (AgentLifecycleState.InReview, AgentLifecycleState.Published) => true,
            (AgentLifecycleState.InReview, AgentLifecycleState.Retired) => true,
            (AgentLifecycleState.Published, AgentLifecycleState.Deprecated) => true,
            (AgentLifecycleState.Published, AgentLifecycleState.Retired) => true,
            (AgentLifecycleState.Deprecated, AgentLifecycleState.Retired) => true,
            (AgentLifecycleState.Deprecated, AgentLifecycleState.Published) => true,
            _ => false
        };

    public static void Assert(AgentLifecycleState from, AgentLifecycleState to)
    {
        if (from == to) return;
        if (!CanTransition(from, to))
            throw new InvalidOperationException($"Illegal agent lifecycle transition: {from} -> {to}.");
    }
}
