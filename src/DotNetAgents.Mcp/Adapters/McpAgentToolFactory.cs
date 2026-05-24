// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.Tools;
using DotNetAgents.Mcp.Abstractions;
using DotNetAgents.Mcp.Models;

namespace DotNetAgents.Mcp.Adapters;

/// <summary>
/// Default <see cref="IMcpAgentToolFactory"/>. Constructs <see cref="McpAgentTool"/> instances
/// against the workspace's <see cref="IMcpClientFactory"/>.
/// </summary>
public sealed class McpAgentToolFactory : IMcpAgentToolFactory
{
    private readonly IMcpClientFactory _clientFactory;

    public McpAgentToolFactory(IMcpClientFactory clientFactory)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
    }

    public ITool BuildAgentTool(McpToolDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new McpAgentTool(definition, _clientFactory);
    }
}
