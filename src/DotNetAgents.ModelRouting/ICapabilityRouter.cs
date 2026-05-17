namespace DotNetAgents.ModelRouting;

/// <summary>Routes by matching required capabilities to model capabilities. FR-MR-002.</summary>
public interface ICapabilityRouter : IModelRouter
{
    /// <summary>Registers a model and its capabilities. Platform supplies model list.</summary>
    void Register(ModelCapabilities model);

    /// <summary>Returns the list of registered models (for tests and diagnostics).</summary>
    IReadOnlyList<ModelCapabilities> GetRegisteredModels();
}
