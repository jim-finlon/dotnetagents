namespace DotNetAgents.Abstractions.Agents;

/// <summary>
/// Identifies one configuration input bound to an agent instance.
/// </summary>
/// <param name="Key">Stable configuration key, such as model.primary or prompt.system.</param>
/// <param name="SourceKind">Where the configuration value came from.</param>
/// <param name="SourceRef">Optional non-secret reference to the source record.</param>
/// <param name="ValueHash">Optional hash/fingerprint of the resolved value. Never store raw secrets here.</param>
/// <param name="Precedence">Higher values win when multiple bindings target the same key.</param>
public sealed record AgentConfigurationBinding(
    string Key,
    AgentConfigurationSourceKind SourceKind,
    string? SourceRef = null,
    string? ValueHash = null,
    int Precedence = 0)
{
    /// <summary>
    /// Validates required fields and safe metadata shape.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Key))
        {
            throw new ArgumentException("Configuration binding key is required.", nameof(Key));
        }

        if (Precedence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Precedence), "Precedence cannot be negative.");
        }
    }
}
