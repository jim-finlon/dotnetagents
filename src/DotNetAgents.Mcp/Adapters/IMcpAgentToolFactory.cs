using DotNetAgents.Abstractions.Tools;
using DotNetAgents.Mcp.Models;

namespace DotNetAgents.Mcp.Adapters;

/// <summary>
/// Builds <see cref="ITool"/> wrappers around <see cref="McpToolDefinition"/> instances so
/// agent executors (e.g. the SDLC supervisor's Phase 6 specialist sub-loop, see story RW-3
/// 574756a4) can invoke MCP tools through the standard <see cref="ITool"/> contract.
/// Per RW-5 (story e05c7b1e). The factory is a generic primitive — it has no knowledge of
/// per-role allowlists or forbidden-tool deny-lists; those policies live in the consuming layer
/// (e.g. WorkflowService's <c>IRoleScopedMcpToolProvider</c>).
/// </summary>
public interface IMcpAgentToolFactory
{
    /// <summary>
    /// Wrap one MCP tool definition as a callable <see cref="ITool"/>. The returned instance
    /// resolves the underlying <c>IMcpClient</c> for the tool's <see cref="McpToolDefinition.ServiceName"/>
    /// on each call, so a service that's added or removed at runtime takes effect on the next call
    /// without re-issuing the wrapped tool.
    /// </summary>
    ITool BuildAgentTool(McpToolDefinition definition);
}
