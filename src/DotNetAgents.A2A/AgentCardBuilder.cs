namespace DotNetAgents.A2A;

/// <summary>Builds an <see cref="AgentCard"/> from agent metadata. FR-A2A-001 (card generation from agent metadata).</summary>
public static class AgentCardBuilder
{
    /// <summary>Creates an agent card from metadata (name, description, skills, optional version and modes).</summary>
    public static AgentCard FromMetadata(
        string name,
        string description,
        IReadOnlyList<Skill> skills,
        string? version = null,
        IReadOnlyList<string>? supportedModes = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(skills);
        return new AgentCard
        {
            Name = name,
            Description = description,
            Skills = skills,
            Version = version ?? "1.0",
            SupportedModes = supportedModes ?? Array.Empty<string>()
        };
    }
}
