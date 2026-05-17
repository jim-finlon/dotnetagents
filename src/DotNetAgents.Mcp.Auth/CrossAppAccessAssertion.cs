using System.Text.Json.Serialization;

namespace DotNetAgents.Mcp.Auth;

/// <summary>
/// Cross App Access assertion — the November 2025 MCP spec's mechanism for letting an upstream
/// MCP server forward a user's identity to a downstream MCP server without re-prompting the
/// user for consent.
/// </summary>
/// <remarks>
/// <para>
/// The shape mirrors a standard RFC 7523 JWT bearer assertion plus MCP-specific extensions for
/// the originating client metadata URL and the bounded scope of the forwarding.
/// </para>
/// <para>
/// DNA does not include a JWT signing implementation in this package — callers integrate with
/// their existing IDP / token service. This package owns the parsing + validation of the claim
/// envelope and the operator policy that allows it.
/// </para>
/// </remarks>
public sealed class CrossAppAccessAssertion
{
    /// <summary>Issuer of the assertion (the upstream MCP server's stable URL).</summary>
    [JsonPropertyName("iss")]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Subject — typically a user identifier the upstream MCP server has authenticated.</summary>
    [JsonPropertyName("sub")]
    public string Subject { get; set; } = string.Empty;

    /// <summary>Audience — the downstream MCP server's identifier.</summary>
    [JsonPropertyName("aud")]
    public string Audience { get; set; } = string.Empty;

    /// <summary>Expiration unix timestamp (seconds).</summary>
    [JsonPropertyName("exp")]
    public long ExpirationUnix { get; set; }

    /// <summary>Issued-at unix timestamp (seconds).</summary>
    [JsonPropertyName("iat")]
    public long IssuedAtUnix { get; set; }

    /// <summary>JWT id (replay prevention).</summary>
    [JsonPropertyName("jti")]
    public string JwtId { get; set; } = string.Empty;

    /// <summary>Original client_id_metadata URL (CIMD) of the requesting client.</summary>
    [JsonPropertyName("mcp_client_metadata_url")]
    public string ClientMetadataUrl { get; set; } = string.Empty;

    /// <summary>Bounded list of MCP scopes the downstream server is authorized to honor on this assertion.</summary>
    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    /// <summary>Bounded list of MCP tool ids that may be invoked under this assertion.</summary>
    [JsonPropertyName("mcp_allowed_tools")]
    public IList<string> AllowedTools { get; set; } = new List<string>();
}
