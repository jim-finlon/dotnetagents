namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// Hosting-side knobs for the November 2025 MCP authentication adapter. Bind from configuration
/// at section <see cref="SectionName"/>. Pairs with <see cref="DotNetAgents.Mcp.Auth.McpAuthOptions"/>
/// (which owns PKCE / CIMD / Cross App Access policy).
/// </summary>
public sealed class McpAuthHostingOptions
{
    public const string SectionName = "DotNetAgents:Mcp:Server:Auth";

    /// <summary>Effective enforcement mode. Defaults to Both (rollout-friendly).</summary>
    public McpAuthMode Mode { get; set; } = McpAuthMode.Both;

    /// <summary>
    /// Operator-readable issuer URL advertised in /.well-known/oauth-authorization-server.
    /// Should be the public origin of this MCP server (e.g. <c>https://sdlc-agent.tyr.local</c>).
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Path the token-exchange endpoint is mapped at. Default <c>/.mcp/oauth/token</c>.</summary>
    public string TokenEndpointPath { get; set; } = "/.mcp/oauth/token";

    /// <summary>Path the authorization endpoint is mapped at. Default <c>/.mcp/oauth/authorize</c>.</summary>
    public string AuthorizationEndpointPath { get; set; } = "/.mcp/oauth/authorize";

    /// <summary>How long an issued PKCE challenge may be redeemed before expiring. Default 5 minutes per RFC 7636 §4.4.</summary>
    public TimeSpan ChallengeLifetime { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// When <c>true</c> the discovery endpoint advertises Cross App Access support. Defaults to
    /// <c>false</c>; operators flip per-service when they want to accept forwarded tokens.
    /// </summary>
    public bool AdvertiseCrossAppAccess { get; set; }
}
