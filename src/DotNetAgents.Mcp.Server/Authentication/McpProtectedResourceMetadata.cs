using System.Text.Json.Serialization;

namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// RFC 9728 OAuth 2.0 Protected Resource Metadata document. Served at
/// <c>/.well-known/oauth-protected-resource</c> by
/// <see cref="McpProtectedResourceEndpointExtensions"/>. Tells MCP clients which authorization
/// server to use when they receive a 401 from this resource.
/// </summary>
public sealed class McpProtectedResourceMetadata
{
    /// <summary>The protected resource identifier (typically the resource server's URL).</summary>
    [JsonPropertyName("resource")]
    public string Resource { get; set; } = string.Empty;

    /// <summary>Authorization servers that can issue tokens for this resource.</summary>
    [JsonPropertyName("authorization_servers")]
    public IList<string> AuthorizationServers { get; set; } = new List<string>();

    /// <summary>RFC 9728 §2: how clients present bearer tokens — DNA accepts the Authorization header.</summary>
    [JsonPropertyName("bearer_methods_supported")]
    public IList<string> BearerMethodsSupported { get; set; } = new List<string> { "header" };

    /// <summary>MCP transport profile this resource exposes.</summary>
    [JsonPropertyName("mcp_transport_profile")]
    public string McpTransportProfile { get; set; } = "streamable-http";

    /// <summary>MCP spec revision this resource claims compliance with.</summary>
    [JsonPropertyName("mcp_protocol_version")]
    public string McpProtocolVersion { get; set; } = "2025-11-25";
}
