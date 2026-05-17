using DotNetAgents.Abstractions.Tools;

namespace DotNetAgents.AgentFramework.Adapters;

/// <summary>
/// Adapter interface for converting DotNetAgents tools to Microsoft Agent Framework tools.
/// </summary>
/// <remarks>
/// This interface will be implemented when Microsoft Agent Framework APIs stabilize.
/// It provides a bridge between DotNetAgents ITool interface and MAF tool system.
/// </remarks>
public interface IToolAdapter
{
    /// <summary>
    /// Converts a DotNetAgents tool to a MAF-compatible tool.
    /// </summary>
    /// <param name="dotNetAgentsTool">The DotNetAgents tool to convert.</param>
    /// <returns>A MAF-compatible tool representation.</returns>
    object ConvertToMAFTool(ITool dotNetAgentsTool);

    /// <summary>
    /// Registers DotNetAgents tools with a MAF tool registry.
    /// </summary>
    /// <param name="tools">The DotNetAgents tools to register.</param>
    /// <param name="mafRegistry">The MAF tool registry.</param>
    void RegisterTools(IEnumerable<ITool> tools, object mafRegistry);
}
