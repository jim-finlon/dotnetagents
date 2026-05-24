// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// Sentinel registered by <see cref="McpAuthServerExtensions.AddMcpAuthServer"/>. Other parts of
/// the server transport check for it to know whether the host opted into November 2025 MCP
/// authentication, so they can stand down conflicting compatibility stubs (Cursor 3.x
/// well-known probe, etc.).
/// </summary>
public sealed class McpAuthEnabledMarker { }
