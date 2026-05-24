using System.ComponentModel.DataAnnotations;

namespace DotNetAgents.Hosting;

/// <summary>
/// MCP host-profile options bound by <see cref="DnaMcpHostExtensions.AddDnaMcpHost"/>.
/// Services own their tool implementations; this profile owns the common host paths.
/// </summary>
public sealed class DnaMcpHostOptions
{
    /// <summary>Configuration section path used by AddDnaMcpHost when binding.</summary>
    public const string SectionPath = "DotNetAgents:Hosting:Mcp";

    /// <summary>Stable MCP service name, e.g. <c>planning_tools</c>. Required.</summary>
    [Required(AllowEmptyStrings = false)]
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>Operator-facing server display name shown to MCP clients.</summary>
    public string ServerDisplayName { get; set; } = string.Empty;

    /// <summary>Server version advertised through Streamable HTTP initialization.</summary>
    public string ServerVersion { get; set; } = "1.0.0";

    /// <summary>Streamable HTTP route path. Default <c>/mcp</c>.</summary>
    public string StreamableHttpPath { get; set; } = "/mcp";

    /// <summary>Legacy REST MCP route prefix. Default <c>/mcp</c>.</summary>
    public string LegacyRestPath { get; set; } = "/mcp";

    /// <summary>Bootstrap instructions route. Default <c>/mcp/instructions</c>.</summary>
    public string InstructionsPath { get; set; } = "/mcp/instructions";

    /// <summary>MCP auth-mode configuration section. Default <c>DotNetAgents:Mcp:Server:Auth</c>.</summary>
    public string AuthModeSection { get; set; } = "DotNetAgents:Mcp:Server:Auth";

    /// <summary>When true, service-owned mutating MCP calls must enforce their policy gate.</summary>
    public bool RequireMutatingOperationPolicy { get; set; } = true;
}
