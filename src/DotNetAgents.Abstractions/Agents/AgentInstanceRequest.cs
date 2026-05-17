namespace DotNetAgents.Abstractions.Agents;

/// <summary>
/// Request to deliberately create one configured runtime agent instance.
/// </summary>
/// <typeparam name="TConfiguration">The caller-defined configuration snapshot type.</typeparam>
public sealed record AgentInstanceRequest<TConfiguration>
{
    /// <summary>
    /// Gets the identity to assign to the runtime instance.
    /// </summary>
    public required AgentInstanceIdentity Identity { get; init; }

    /// <summary>
    /// Gets the resolved configuration snapshot for this instance.
    /// </summary>
    public required TConfiguration Configuration { get; init; }

    /// <summary>
    /// Gets configuration provenance bindings used to resolve <see cref="Configuration"/>.
    /// </summary>
    public IReadOnlyList<AgentConfigurationBinding> ConfigurationBindings { get; init; } =
        Array.Empty<AgentConfigurationBinding>();

    /// <summary>
    /// Gets optional model binding names such as primary, critic, or embedder.
    /// </summary>
    public IReadOnlyDictionary<string, string> ModelBindings { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Gets optional tool binding names or registry references.
    /// </summary>
    public IReadOnlyDictionary<string, string> ToolBindings { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Gets correlation metadata for logs, traces, SDLC, and lab evidence.
    /// </summary>
    public AgentRuntimeCorrelation Correlation { get; init; } = new();

    /// <summary>
    /// Gets the time the runtime request was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Validates the request shape.
    /// </summary>
    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(Identity);
        ArgumentNullException.ThrowIfNull(Configuration);

        Identity.Validate();

        foreach (var binding in ConfigurationBindings)
        {
            ArgumentNullException.ThrowIfNull(binding);
            binding.Validate();
        }
    }
}
