using System.Text.Json.Serialization;

namespace DotNetAgents.Mcp.Auth;

/// <summary>
/// Client ID Metadata Document (CIMD) per the November 2025 MCP spec — a publicly-resolvable
/// JSON document at the URL the client uses as its <c>client_id</c>. The MCP server fetches
/// this document on first contact instead of relying on pre-registered client IDs.
/// </summary>
/// <remarks>
/// <para>
/// Field shape mirrors RFC 7591 (OAuth Dynamic Client Registration) so existing IDPs can
/// re-use their registration documents as CIMDs. DNA validates the document against
/// <see cref="McpAuthOptions"/> before trusting it.
/// </para>
/// <para>
/// Required at minimum: <see cref="ClientId"/> matches the URL the document was fetched from,
/// <see cref="RedirectUris"/> contains at least one HTTPS URI, and
/// <see cref="GrantTypes"/> includes <c>authorization_code</c> with PKCE.
/// </para>
/// </remarks>
public sealed class ClientMetadataDocument
{
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("client_name")]
    public string? ClientName { get; set; }

    [JsonPropertyName("client_uri")]
    public string? ClientUri { get; set; }

    [JsonPropertyName("redirect_uris")]
    public IList<string> RedirectUris { get; set; } = new List<string>();

    [JsonPropertyName("grant_types")]
    public IList<string> GrantTypes { get; set; } = new List<string>();

    [JsonPropertyName("response_types")]
    public IList<string> ResponseTypes { get; set; } = new List<string>();

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("token_endpoint_auth_method")]
    public string? TokenEndpointAuthMethod { get; set; }

    [JsonPropertyName("software_id")]
    public string? SoftwareId { get; set; }

    [JsonPropertyName("software_version")]
    public string? SoftwareVersion { get; set; }

    [JsonPropertyName("contacts")]
    public IList<string> Contacts { get; set; } = new List<string>();

    [JsonPropertyName("policy_uri")]
    public string? PolicyUri { get; set; }

    /// <summary>MCP-specific extension: the supported MCP transport profile (typically <c>"streamable-http"</c>).</summary>
    [JsonPropertyName("mcp_transport_profile")]
    public string? McpTransportProfile { get; set; }
}
