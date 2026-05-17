using DotNetAgents.Ecosystem;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.Swarm;

/// <summary>
/// Plugin for Swarm Intelligence functionality.
/// </summary>
public class SwarmPlugin : PluginBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SwarmPlugin"/> class.
    /// </summary>
    public SwarmPlugin()
    {
        Metadata = new PluginMetadata
        {
            Id = "swarm",
            Name = "Swarm Intelligence",
            Version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0",
            Description = "Provides swarm intelligence algorithms for multi-agent task coordination: Particle Swarm Optimization (PSO), Ant Colony Optimization (ACO), Flocking, and Consensus. (Agent genome evolution is in DotNetAgents.Agents.Evolutionary, not here.)",
            Author = "DotNetAgents",
            License = "MIT",
            Category = "Agent Capabilities",
            Tags = new List<string> { "swarm", "coordination", "optimization", "multi-agent" },
            Dependencies = new List<string>(),
            RepositoryUrl = "https://github.com/dotnetagents/DotNetAgents",
            DocumentationUrl = "https://github.com/dotnetagents/DotNetAgents/docs/guides/swarm-intelligence.md"
        };
    }

    /// <inheritdoc />
    protected override Task OnInitializeAsync(IPluginContext context, CancellationToken cancellationToken)
    {
        Logger?.LogInformation(
            "Swarm Intelligence plugin initialized. Swarm coordinators can now be created and used.");

        return Task.CompletedTask;
    }
}
