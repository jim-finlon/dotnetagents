namespace DotNetAgents.Voice.IntentClassification;

/// <summary>
/// Represents a domain definition with its intents and metadata.
/// </summary>
public class DomainDefinition
{
    /// <summary>
    /// Gets the domain name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the description of the domain.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets the mapping of action names to their descriptions.
    /// </summary>
    public Dictionary<string, string> ActionDescriptions { get; } = new();

    /// <summary>
    /// Gets the dictionary of intent definitions keyed by full intent name.
    /// </summary>
    public Dictionary<string, IntentDefinition> Intents { get; } = new();

    /// <summary>
    /// Gets the target MCP service name for this domain.
    /// </summary>
    public string? TargetService { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainDefinition"/> class.
    /// </summary>
    /// <param name="name">The domain name.</param>
    public DomainDefinition(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
