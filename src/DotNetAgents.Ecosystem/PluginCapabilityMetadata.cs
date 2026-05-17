namespace DotNetAgents.Ecosystem;

/// <summary>
/// Indicates where a provider or plugin normally executes.
/// </summary>
public enum PluginDeploymentKind
{
    /// <summary>
    /// The package has not declared its execution location.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The package calls a cloud-hosted service.
    /// </summary>
    Cloud = 1,

    /// <summary>
    /// The package targets a local runtime or local network endpoint.
    /// </summary>
    Local = 2,

    /// <summary>
    /// The package can target either local or cloud runtimes.
    /// </summary>
    Hybrid = 3
}

/// <summary>
/// Non-secret credential reference expected by a provider or plugin.
/// </summary>
/// <param name="Category">CredentialsAgent category, such as <c>providers/openai</c>.</param>
/// <param name="Name">CredentialsAgent credential name, such as <c>api_key</c>.</param>
/// <param name="Required">Whether the credential is required for normal operation.</param>
/// <param name="Description">Human-readable description that must not include a secret value.</param>
public sealed record PluginCredentialExpectation(
    string Category,
    string Name,
    bool Required = true,
    string? Description = null);

/// <summary>
/// Stable capability metadata used by provider and plugin registration tests.
/// </summary>
/// <param name="ProviderId">Stable provider or plugin id.</param>
/// <param name="SupportedModalities">Declared modalities such as text, vision, audio, or embedding.</param>
/// <param name="SupportsStreaming">Whether streaming output is supported by the package surface.</param>
/// <param name="SupportsToolCalling">Whether tool/function calling is supported by the package surface.</param>
/// <param name="DeploymentKind">Cloud/local classification.</param>
/// <param name="CredentialExpectations">Non-secret CredentialsAgent references required by the package.</param>
/// <param name="DefaultModelConfigurationKey">Optional options/configuration key used for the default model or deployment.</param>
public sealed record PluginCapabilityMetadata(
    string ProviderId,
    IReadOnlyList<string> SupportedModalities,
    bool SupportsStreaming,
    bool SupportsToolCalling,
    PluginDeploymentKind DeploymentKind,
    IReadOnlyList<PluginCredentialExpectation> CredentialExpectations,
    string? DefaultModelConfigurationKey = null);

/// <summary>
/// Implemented by plugins that expose provider/plugin capability metadata.
/// </summary>
public interface IPluginWithCapabilityMetadata : IPluginWithMetadata
{
    /// <summary>
    /// Gets non-secret capability metadata for registration, routing, and docs.
    /// </summary>
    PluginCapabilityMetadata CapabilityMetadata { get; }
}
