namespace DotNetAgents.Voice.IntentClassification;

/// <summary>
/// Represents a definition of an intent with its parameters and metadata.
/// </summary>
public record IntentDefinition
{
    /// <summary>
    /// Gets the domain of the intent (e.g., "goals", "activities").
    /// </summary>
    public required string Domain { get; init; }

    /// <summary>
    /// Gets the action of the intent (e.g., "create", "complete").
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Gets the optional sub-type of the intent (e.g., "lifetime", "personal").
    /// </summary>
    public string? SubType { get; init; }

    /// <summary>
    /// Gets the required parameters for this intent.
    /// </summary>
    public IReadOnlyList<string> RequiredParameters { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the optional parameters for this intent.
    /// </summary>
    public IReadOnlyList<string> OptionalParameters { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the description of this intent.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the full intent name (domain.action or domain.action.subtype).
    /// </summary>
    public string FullName => string.IsNullOrEmpty(SubType)
        ? $"{Domain}.{Action}"
        : $"{Domain}.{Action}.{SubType}";

    /// <summary>
    /// Gets the intent name with underscore format (domain.action_subtype).
    /// </summary>
    public string FullNameUnderscore => string.IsNullOrEmpty(SubType)
        ? $"{Domain}.{Action}"
        : $"{Domain}.{Action}_{SubType}";
}
