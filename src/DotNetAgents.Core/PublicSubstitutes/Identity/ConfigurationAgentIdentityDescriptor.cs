using DotNetAgents.Abstractions.PublicSubstitutes.Identity;
using Microsoft.Extensions.Configuration;

namespace DotNetAgents.Core.PublicSubstitutes.Identity;

/// <summary>
/// Local public adapter that reads agent identity from configuration and falls
/// back to a safe development identity when configuration is absent.
/// </summary>
public sealed class ConfigurationAgentIdentityDescriptor : IAgentIdentityDescriptor
{
    private const string SectionName = "Dna:Identity";
    private static readonly AgentIdentity DefaultIdentity =
        new("local-public-agent", "Local Public Agent", "WorkstationSession");

    public ConfigurationAgentIdentityDescriptor(IConfiguration? configuration)
    {
        Current = CreateIdentity(configuration);
    }

    /// <inheritdoc />
    public AgentIdentity Current { get; }

    private static AgentIdentity CreateIdentity(IConfiguration? configuration)
    {
        if (configuration is null)
        {
            return DefaultIdentity;
        }

        var section = configuration.GetSection(SectionName);
        return new AgentIdentity(
            Clean(section["ActorId"]) ?? DefaultIdentity.ActorId,
            Clean(section["DisplayName"]) ?? DefaultIdentity.DisplayName,
            Clean(section["ActorType"]) ?? DefaultIdentity.ActorType,
            Clean(section["Capability"]));
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
