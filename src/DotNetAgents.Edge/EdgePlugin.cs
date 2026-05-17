using DotNetAgents.Ecosystem;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Edge;

/// <summary>
/// Plugin for Edge Computing functionality.
/// </summary>
public class EdgePlugin : PluginBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EdgePlugin"/> class.
    /// </summary>
    public EdgePlugin()
    {
        Metadata = new PluginMetadata
        {
            Id = "edge",
            Name = "Edge Computing",
            Version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0",
            Description = "Provides edge computing capabilities for running agents on mobile and offline devices. Supports offline caching, local model execution, and synchronization with cloud services.",
            Author = "DotNetAgents",
            License = "MIT",
            Category = "Infrastructure",
            Tags = new List<string> { "edge", "mobile", "offline", "caching" },
            Dependencies = new List<string>(),
            RepositoryUrl = "https://github.com/dotnetagents/DotNetAgents",
            DocumentationUrl = "https://github.com/dotnetagents/DotNetAgents/docs/guides/edge-computing.md"
        };
    }

    /// <inheritdoc />
    protected override Task OnInitializeAsync(IPluginContext context, CancellationToken cancellationToken)
    {
        Logger?.LogInformation(
            "Edge Computing plugin initialized. Edge agents can now be configured and used.");

        return Task.CompletedTask;
    }
}
