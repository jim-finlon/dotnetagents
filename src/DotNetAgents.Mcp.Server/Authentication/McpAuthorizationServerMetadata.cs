// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// RFC 8414 OAuth 2.0 Authorization Server Metadata document, with MCP November 2025
/// extensions (CIMD support, Cross App Access support, mandatory PKCE methods). Served at
/// <c>/.well-known/oauth-authorization-server</c> by <see cref="McpDiscoveryEndpointExtensions"/>.
/// </summary>
public sealed class McpAuthorizationServerMetadata
{
    [JsonPropertyName("issuer")]
    public string Issuer { get; set; } = string.Empty;

    [JsonPropertyName("authorization_endpoint")]
    public string AuthorizationEndpoint { get; set; } = string.Empty;

    [JsonPropertyName("token_endpoint")]
    public string TokenEndpoint { get; set; } = string.Empty;

    [JsonPropertyName("response_types_supported")]
    public IList<string> ResponseTypesSupported { get; set; } = new List<string> { "code" };

    [JsonPropertyName("grant_types_supported")]
    public IList<string> GrantTypesSupported { get; set; } = new List<string> { "authorization_code" };

    /// <summary>RFC 7636 §4.2: only S256 advertised — DNA refuses plain.</summary>
    [JsonPropertyName("code_challenge_methods_supported")]
    public IList<string> CodeChallengeMethodsSupported { get; set; } = new List<string> { "S256" };

    /// <summary>MCP November 2025 extension: this server accepts CIMD-form client_id values.</summary>
    [JsonPropertyName("client_id_metadata_documents_supported")]
    public bool ClientIdMetadataDocumentsSupported { get; set; } = true;

    /// <summary>MCP November 2025 extension: bumped when the operator advertises Cross App Access.</summary>
    [JsonPropertyName("mcp_cross_app_access_supported")]
    public bool McpCrossAppAccessSupported { get; set; }

    /// <summary>MCP transport profile this server speaks.</summary>
    [JsonPropertyName("mcp_transport_profile")]
    public string McpTransportProfile { get; set; } = "streamable-http";

    /// <summary>MCP spec revision this server claims compliance with.</summary>
    [JsonPropertyName("mcp_protocol_version")]
    public string McpProtocolVersion { get; set; } = "2025-11-25";
}
