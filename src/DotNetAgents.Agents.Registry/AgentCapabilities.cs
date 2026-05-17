namespace DotNetAgents.Agents.Registry;

/// <summary>
/// Represents an agent's capabilities and metadata.
/// </summary>
public record AgentCapabilities
{
    /// <summary>
    /// Gets the unique identifier of the agent.
    /// </summary>
    public string AgentId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the type of the agent (e.g., "document_processor", "data_analyst").
    /// </summary>
    public string AgentType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the list of tools supported by this agent.
    /// </summary>
    public string[] SupportedTools { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the list of intents supported by this agent.
    /// </summary>
    public string[] SupportedIntents { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the maximum number of concurrent tasks this agent can handle.
    /// </summary>
    public int MaxConcurrentTasks { get; init; } = 1;

    /// <summary>
    /// Gets additional metadata associated with this agent.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}
