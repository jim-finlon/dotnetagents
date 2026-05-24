// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.Agents;

namespace DotNetAgents.Abstractions.CounterAgents;

/// <summary>
/// Marker extension for <see cref="IAgent"/>s that carry a list of counter-agents to consult
/// before executing tool calls or other reviewable actions. Agents that opt in to counter-agent
/// review implement this interface; the middleware dispatches review calls to all registered
/// counter-agents in parallel.
/// </summary>
/// <remarks>
/// Not implementing this interface means an agent runs without counter-agent review — a valid
/// choice for trusted internal agents or test scenarios. Production agents should typically
/// always carry at least the default Budget + Safety counter-agents.
/// </remarks>
public interface IAgentWithCounterAgents : IAgent
{
    /// <summary>The counter-agents this agent consults. Order is informational only — review runs in parallel.</summary>
    IReadOnlyList<ICounterAgent> CounterAgents { get; }
}
