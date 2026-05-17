namespace DotNetAgents.Voice.IntentClassification;

/// <summary>
/// Interface for registering intents and domains dynamically at runtime.
/// </summary>
public interface IIntentTaxonomyRegistry
{
    /// <summary>
    /// Registers a new intent with the taxonomy.
    /// </summary>
    /// <param name="domain">The domain of the intent (e.g., "goals", "activities").</param>
    /// <param name="action">The action of the intent (e.g., "create", "complete").</param>
    /// <param name="subType">Optional sub-type of the intent (e.g., "lifetime", "personal").</param>
    /// <param name="requiredParameters">Required parameters for this intent.</param>
    /// <param name="optionalParameters">Optional parameters for this intent.</param>
    /// <param name="description">Optional description of the intent.</param>
    void RegisterIntent(
        string domain,
        string action,
        string? subType,
        string[] requiredParameters,
        string[]? optionalParameters = null,
        string? description = null);

    /// <summary>
    /// Registers a new domain with the taxonomy.
    /// </summary>
    /// <param name="domain">The domain name.</param>
    /// <param name="description">Optional description of the domain.</param>
    /// <param name="targetService">Optional target MCP service name for this domain.</param>
    /// <param name="actionDescriptions">Optional mapping of action names to descriptions.</param>
    void RegisterDomain(
        string domain,
        string? description = null,
        string? targetService = null,
        Dictionary<string, string>? actionDescriptions = null);

    /// <summary>
    /// Gets the current taxonomy snapshot.
    /// </summary>
    /// <returns>The intent taxonomy.</returns>
    IntentTaxonomy GetTaxonomy();

    /// <summary>
    /// Gets the required parameters for a specific intent.
    /// </summary>
    /// <param name="domain">The domain.</param>
    /// <param name="action">The action.</param>
    /// <param name="subType">Optional sub-type.</param>
    /// <returns>The list of required parameters, or empty if not found.</returns>
    IReadOnlyList<string> GetRequiredParameters(string domain, string action, string? subType = null);

    /// <summary>
    /// Gets the optional parameters for a specific intent.
    /// </summary>
    /// <param name="domain">The domain.</param>
    /// <param name="action">The action.</param>
    /// <param name="subType">Optional sub-type.</param>
    /// <returns>The list of optional parameters, or empty if not found.</returns>
    IReadOnlyList<string> GetOptionalParameters(string domain, string action, string? subType = null);

    /// <summary>
    /// Gets the target MCP service for a domain.
    /// </summary>
    /// <param name="domain">The domain name.</param>
    /// <returns>The target service name, or null if not found.</returns>
    string? GetTargetService(string domain);
}
