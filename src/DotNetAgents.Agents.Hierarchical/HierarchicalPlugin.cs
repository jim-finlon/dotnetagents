using DotNetAgents.Ecosystem;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.Hierarchical;

/// <summary>
/// Plugin for Hierarchical Agent Organization functionality.
/// </summary>
public class HierarchicalPlugin : PluginBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HierarchicalPlugin"/> class.
    /// </summary>
    public HierarchicalPlugin()
    {
        Metadata = new PluginMetadata
        {
            Id = "hierarchical",
            Name = "Hierarchical Agents",
            Version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0",
            Description = "Provides hierarchical organization of agents with parent-child relationships. Enables tree-structured agent hierarchies for complex multi-agent systems.",
            Author = "DotNetAgents",
            License = "MIT",
            Category = "Agent Capabilities",
            Tags = new List<string> { "hierarchical", "organization", "multi-agent", "tree" },
            Dependencies = new List<string>(),
            RepositoryUrl = "https://github.com/dotnetagents/DotNetAgents",
            DocumentationUrl = "https://github.com/dotnetagents/DotNetAgents/docs/guides/hierarchical-agents.md"
        };
    }

    /// <inheritdoc />
    protected override Task OnInitializeAsync(IPluginContext context, CancellationToken cancellationToken)
    {
        Logger?.LogInformation(
            "Hierarchical Agents plugin initialized. Hierarchical agent organizations can now be created and used.");

        return Task.CompletedTask;
    }
}
