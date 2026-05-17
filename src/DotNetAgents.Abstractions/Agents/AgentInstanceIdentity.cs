namespace DotNetAgents.Abstractions.Agents;

/// <summary>
/// Stable identity for one deliberate runtime instance of an agent species.
/// </summary>
public sealed record AgentInstanceIdentity
{
    /// <summary>
    /// Gets the agent species or durable class of behavior.
    /// </summary>
    public required string SpeciesId { get; init; }

    /// <summary>
    /// Gets the unique runtime instance identifier.
    /// </summary>
    public required string InstanceId { get; init; }

    /// <summary>
    /// Gets the human-readable runtime instance name.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets the evaluated phenotype identifier when this instance came from an evolutionary run.
    /// </summary>
    public string? PhenotypeId { get; init; }

    /// <summary>
    /// Gets the parent instance identifier when this instance was cloned or mutated.
    /// </summary>
    public string? ParentInstanceId { get; init; }

    /// <summary>
    /// Creates an identity with an explicit instance id and display name.
    /// </summary>
    public static AgentInstanceIdentity Create(
        string speciesId,
        string instanceId,
        string displayName,
        string? phenotypeId = null,
        string? parentInstanceId = null)
    {
        var identity = new AgentInstanceIdentity
        {
            SpeciesId = speciesId,
            InstanceId = instanceId,
            DisplayName = displayName,
            PhenotypeId = phenotypeId,
            ParentInstanceId = parentInstanceId
        };

        identity.Validate();
        return identity;
    }

    /// <summary>
    /// Validates required identity fields.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SpeciesId))
        {
            throw new ArgumentException("Agent species id is required.", nameof(SpeciesId));
        }

        if (string.IsNullOrWhiteSpace(InstanceId))
        {
            throw new ArgumentException("Agent instance id is required.", nameof(InstanceId));
        }

        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            throw new ArgumentException("Agent display name is required.", nameof(DisplayName));
        }
    }
}
