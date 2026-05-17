namespace DotNetAgents.Abstractions.PublicSubstitutes.Lab;

/// <summary>
/// Provides the public-safe advisory posture of the current lab environment.
/// This surface deliberately excludes lab control, sandbox enforcement, and
/// experiment orchestration internals.
/// </summary>
public interface ILabEnvironmentDescriptor
{
    /// <summary>Gets the cached advisory posture for the current environment.</summary>
    LabEnvironment Current { get; }
}
