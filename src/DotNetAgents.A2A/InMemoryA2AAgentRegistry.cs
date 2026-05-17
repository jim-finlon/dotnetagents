namespace DotNetAgents.A2A;

/// <summary>In-memory A2A agent registry with capability search and health. A2A-4.3.</summary>
public sealed class InMemoryA2AAgentRegistry : IA2AAgentRegistry
{
    private readonly Dictionary<string, Entry> _agents = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public void Register(string id, IA2AAgent agent, AgentRegistrationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(agent);
        _agents[id] = new Entry(agent, options);
    }

    /// <inheritdoc />
    public bool Unregister(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return _agents.Remove(id);
    }

    /// <inheritdoc />
    public IReadOnlyList<IA2AAgent> FindByCapability(string capabilityName)
    {
        if (string.IsNullOrWhiteSpace(capabilityName))
            return Array.Empty<IA2AAgent>();
        var name = capabilityName.Trim();
        var list = new List<IA2AAgent>();
        foreach (var (_, entry) in _agents)
        {
            var card = entry.Agent.GetAgentCard();
            var hasSkill = card.Skills.Any(s =>
                string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase) ||
                (s.Description.Contains(name, StringComparison.OrdinalIgnoreCase)));
            if (hasSkill)
                list.Add(entry.Agent);
        }
        return list;
    }

    /// <inheritdoc />
    public AgentHealth? GetHealth(string id)
    {
        if (string.IsNullOrEmpty(id) || !_agents.TryGetValue(id, out _))
            return null;
        return new AgentHealth { Status = "Healthy" };
    }

    /// <inheritdoc />
    public IReadOnlyList<string> List() => _agents.Keys.ToList();

    /// <inheritdoc />
    public IA2AAgent? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return _agents.TryGetValue(id, out var entry) ? entry.Agent : null;
    }

    private sealed record Entry(IA2AAgent Agent, AgentRegistrationOptions? Options);
}
