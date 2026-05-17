namespace DotNetAgents.ModelRouting;

/// <summary>Cascade router: try tiers in order, escalate on low confidence. FR-MR-001.</summary>
public interface ICascadeRouter : IModelRouter
{
    /// <summary>Adds a tier: try this model first; if confidence &lt; threshold, escalate to next.</summary>
    /// <param name="modelId">Model identifier for this tier.</param>
    /// <param name="confidenceThreshold">Minimum confidence (0–1) to accept; below this, try next tier.</param>
    /// <returns>This router for fluent configuration.</returns>
    ICascadeRouter AddTier(string modelId, double confidenceThreshold);
}
