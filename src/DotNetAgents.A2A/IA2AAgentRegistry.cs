// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.A2A;

/// <summary>Registry for A2A agents: capability-based lookup, health, optional trust. A2A-4.3.</summary>
public interface IA2AAgentRegistry
{
    /// <summary>Registers an agent under the given id.</summary>
    void Register(string id, IA2AAgent agent, AgentRegistrationOptions? options = null);

    /// <summary>Unregisters the agent.</summary>
    bool Unregister(string id);

    /// <summary>Finds agents that expose a skill or capability matching the given name (case-insensitive).</summary>
    IReadOnlyList<IA2AAgent> FindByCapability(string capabilityName);

    /// <summary>Gets health for the agent (if supported).</summary>
    AgentHealth? GetHealth(string id);

    /// <summary>Lists all registered agent ids.</summary>
    IReadOnlyList<string> List();

    /// <summary>
    /// Get an agent by its registered id. Returns null when not found. Added in story c46e33de
    /// — A2A 1.0 server adapter needs id-based agent lookup for the
    /// <c>/.well-known/agent.json</c> endpoint.
    /// </summary>
    IA2AAgent? GetById(string id);
}

/// <summary>Options when registering an agent. A2A-4.3.</summary>
public sealed record AgentRegistrationOptions
{
    /// <summary>Optional trust level (e.g. 0-1 or tier).</summary>
    public double? TrustLevel { get; init; }
}

/// <summary>Health status of an agent. A2A-4.3.</summary>
public sealed record AgentHealth
{
    public string Status { get; init; } = "Healthy";
    public string? Message { get; init; }
}
