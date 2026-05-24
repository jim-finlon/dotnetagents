// SPDX-License-Identifier: Apache-2.0

using Microsoft.AspNetCore.Builder;

namespace DotNetAgents.Mcp.Server;

/// <summary>
/// Optional hook for Cursor/browser MCP CORS. Default is no-op; hosts keep <see cref="WebApplication"/> CORS policies below.
/// Override in a fork only if Tyr/LAN origins need explicit allow-listing for Streamable HTTP MCP.
/// </summary>
public static class CursorMcpCorsExtensions
{
    public static WebApplication UseCursorMcpCors(this WebApplication app) => app;
}
