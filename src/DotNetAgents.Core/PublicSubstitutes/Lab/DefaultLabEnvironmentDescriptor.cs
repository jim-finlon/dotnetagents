using DotNetAgents.Abstractions.PublicSubstitutes.Lab;

namespace DotNetAgents.Core.PublicSubstitutes.Lab;

/// <summary>
/// Local public adapter for non-sandboxed development and sample hosts.
/// </summary>
public sealed class DefaultLabEnvironmentDescriptor : ILabEnvironmentDescriptor
{
    public static readonly LabEnvironment LocalProcess =
        new("local-process", NetworkEgressAllowed: true, FileSystemWriteAllowed: true);

    /// <inheritdoc />
    public LabEnvironment Current => LocalProcess;
}
