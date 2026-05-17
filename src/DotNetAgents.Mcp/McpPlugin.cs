using DotNetAgents.Ecosystem;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Mcp;

/// <summary>
/// Plugin for Model Context Protocol (MCP) functionality.
/// </summary>
public class McpPlugin : PluginBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="McpPlugin"/> class.
    /// </summary>
    public McpPlugin()
    {
        Metadata = new PluginMetadata
        {
            Id = "mcp",
            Name = "Model Context Protocol",
            Version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0",
            Description = "Provides Model Context Protocol (MCP) client support for connecting to MCP servers. Enables agents to access external tools and resources through the MCP protocol.",
            Author = "DotNetAgents",
            License = "MIT",
            Category = "Integrations",
            Tags = new List<string> { "mcp", "protocol", "tools", "integration" },
            Dependencies = new List<string>(),
            RepositoryUrl = "https://github.com/dotnetagents/DotNetAgents",
            DocumentationUrl = "https://github.com/dotnetagents/DotNetAgents/docs/guides/mcp.md"
        };
    }

    /// <inheritdoc />
    protected override Task OnInitializeAsync(IPluginContext context, CancellationToken cancellationToken)
    {
        Logger?.LogInformation(
            "MCP plugin initialized. MCP clients can now be configured and used.");

        return Task.CompletedTask;
    }
}
