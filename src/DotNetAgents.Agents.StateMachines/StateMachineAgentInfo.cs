using DotNetAgents.Agents.Registry;

namespace DotNetAgents.Agents.StateMachines;

/// <summary>
/// Extension methods for integrating state machines with agent registry.
/// Note: Since AgentInfo is a record without Metadata, we use a separate registry to track state machine states.
/// </summary>
public static class StateMachineAgentInfoExtensions
{
    // Note: AgentInfo doesn't have a Metadata property, so we'll track state machine states
    // separately in AgentStateMachineRegistry. These extension methods are placeholders
    // for future enhancement if AgentInfo gains metadata support.
}
