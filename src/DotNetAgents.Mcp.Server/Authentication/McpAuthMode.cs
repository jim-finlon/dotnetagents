// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// How strictly the MCP server enforces the November 2025 PKCE / CIMD authentication spec.
/// Operators flip per-deployment to migrate clients without breakage.
/// </summary>
public enum McpAuthMode
{
    /// <summary>Legacy auth only (Bearer / API key). Token-exchange endpoint is not exposed.</summary>
    Legacy = 0,

    /// <summary>
    /// Token-exchange endpoint is exposed and accepts PKCE-bearing requests, but legacy auth
    /// continues to work alongside. Default for the rollout window so existing clients keep working.
    /// </summary>
    Both = 1,

    /// <summary>
    /// Strict — token-exchange endpoint REQUIRES PKCE; legacy auth is rejected with 401. Only
    /// flip after confirming all known clients support the November 2025 handshake.
    /// </summary>
    Strict = 2,
}
