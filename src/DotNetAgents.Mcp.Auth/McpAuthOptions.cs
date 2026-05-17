namespace DotNetAgents.Mcp.Auth;

/// <summary>
/// Operator-tunable knobs for MCP November 2025 authentication. Bind from configuration at
/// section <see cref="SectionName"/>.
/// </summary>
public sealed class McpAuthOptions
{
    public const string SectionName = "DotNetAgents:Mcp:Auth";

    /// <summary>When <c>true</c> (default) the server REJECTS any token exchange that does not present a PKCE verifier.</summary>
    public bool RequirePkce { get; set; } = true;

    /// <summary>When <c>true</c> the server requires the client_id to resolve to a CIMD URL (RFC 7591 metadata).</summary>
    public bool RequireCimd { get; set; }

    /// <summary>
    /// Allow-list of host names whose CIMD documents will be trusted. Empty means trust any
    /// HTTPS origin — operators in production should populate this so untrusted CIMD URLs cannot
    /// register dynamically.
    /// </summary>
    public IList<string> AllowedClientMetadataHosts { get; set; } = new List<string>();

    /// <summary>Maximum size (bytes) of a CIMD response. Defaults to 32 KiB.</summary>
    public int MaxClientMetadataBytes { get; set; } = 32 * 1024;

    /// <summary>HTTP timeout for CIMD fetches.</summary>
    public TimeSpan ClientMetadataFetchTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>How long resolved CIMDs are cached before being re-fetched. Defaults to 10 minutes.</summary>
    public TimeSpan ClientMetadataCacheDuration { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>When <c>true</c> the server accepts Cross App Access assertions (token forwarding) from peer MCP servers.</summary>
    public bool AllowCrossAppAccess { get; set; }

    /// <summary>Allow-list of audiences for which Cross App Access tokens may be minted.</summary>
    public IList<string> CrossAppAccessAudiences { get; set; } = new List<string>();
}
