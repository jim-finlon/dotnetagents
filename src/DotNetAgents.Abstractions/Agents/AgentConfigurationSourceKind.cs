namespace DotNetAgents.Abstractions.Agents;

/// <summary>
/// Describes where an agent instance configuration value came from.
/// </summary>
public enum AgentConfigurationSourceKind
{
    /// <summary>
    /// The configuration value came from a framework or species default.
    /// </summary>
    Default = 0,

    /// <summary>
    /// The configuration value came from SDLC story or workflow context.
    /// </summary>
    SdlcStoryContext = 1,

    /// <summary>
    /// The configuration value came from a prompt gene or prompt catalog entry.
    /// </summary>
    PromptGene = 2,

    /// <summary>
    /// The configuration value came from process or host environment settings.
    /// </summary>
    Environment = 3,

    /// <summary>
    /// The configuration value came from an explicit operator override.
    /// </summary>
    OperatorOverride = 4,

    /// <summary>
    /// The configuration value came from a laboratory or experiment overlay.
    /// </summary>
    ExperimentOverlay = 5
}
