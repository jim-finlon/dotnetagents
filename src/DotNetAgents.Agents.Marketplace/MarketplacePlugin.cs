using DotNetAgents.Ecosystem;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.Marketplace;

/// <summary>
/// Plugin for Agent Marketplace functionality.
/// </summary>
public class MarketplacePlugin : PluginBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarketplacePlugin"/> class.
    /// </summary>
    public MarketplacePlugin()
    {
        Metadata = new PluginMetadata
        {
            Id = "marketplace",
            Name = "Agent Marketplace",
            Version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0",
            Description = "Provides an agent marketplace for discovering, sharing, and deploying agents. Enables agent discovery, rating, and distribution across the ecosystem.",
            Author = "DotNetAgents",
            License = "MIT",
            Category = "Agent Capabilities",
            Tags = new List<string> { "marketplace", "discovery", "sharing", "agents" },
            Dependencies = new List<string>(),
            RepositoryUrl = "https://github.com/dotnetagents/DotNetAgents",
            DocumentationUrl = "https://github.com/dotnetagents/DotNetAgents/docs/guides/agent-marketplace.md"
        };
    }

    /// <inheritdoc />
    protected override Task OnInitializeAsync(IPluginContext context, CancellationToken cancellationToken)
    {
        Logger?.LogInformation(
            "Agent Marketplace plugin initialized. Agent marketplace can now be used.");

        return Task.CompletedTask;
    }
}
