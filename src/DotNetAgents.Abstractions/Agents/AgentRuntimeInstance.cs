// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Abstractions.Agents;

/// <summary>
/// A concrete runtime agent paired with the identity, configuration, and correlation metadata used to create it.
/// </summary>
/// <typeparam name="TAgent">The concrete agent type.</typeparam>
/// <typeparam name="TConfiguration">The caller-defined configuration snapshot type.</typeparam>
public sealed record AgentRuntimeInstance<TAgent, TConfiguration>
    where TAgent : IAgent
{
    /// <summary>
    /// Gets the runtime identity.
    /// </summary>
    public required AgentInstanceIdentity Identity { get; init; }

    /// <summary>
    /// Gets the concrete agent instance.
    /// </summary>
    public required TAgent Agent { get; init; }

    /// <summary>
    /// Gets the resolved configuration snapshot.
    /// </summary>
    public required TConfiguration Configuration { get; init; }

    /// <summary>
    /// Gets configuration provenance bindings.
    /// </summary>
    public IReadOnlyList<AgentConfigurationBinding> ConfigurationBindings { get; init; } =
        Array.Empty<AgentConfigurationBinding>();

    /// <summary>
    /// Gets model binding names or references.
    /// </summary>
    public IReadOnlyDictionary<string, string> ModelBindings { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Gets tool binding names or references.
    /// </summary>
    public IReadOnlyDictionary<string, string> ToolBindings { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Gets runtime correlation metadata.
    /// </summary>
    public AgentRuntimeCorrelation Correlation { get; init; } = new();

    /// <summary>
    /// Gets the time the runtime instance was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
