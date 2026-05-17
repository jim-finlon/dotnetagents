using DotNetAgents.Ecosystem;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// Plugin for Behavior Trees functionality.
/// </summary>
public class BehaviorTreesPlugin : PluginBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BehaviorTreesPlugin"/> class.
    /// </summary>
    public BehaviorTreesPlugin()
    {
        Metadata = new PluginMetadata
        {
            Id = "behaviortrees",
            Name = "Behavior Trees",
            Version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0",
            Description = "Provides hierarchical decision-making for autonomous agents using behavior trees. Supports composite nodes (Sequence, Selector, Parallel), decorator nodes (Inverter, Retry, Timeout, Cooldown), and integration with state machines, workflows, and LLMs.",
            Author = "DotNetAgents",
            License = "MIT",
            Category = "Agent Capabilities",
            Tags = new List<string> { "behavior-tree", "decision-making", "autonomous", "agents" },
            Dependencies = new List<string> { "statemachines" },
            RepositoryUrl = "https://github.com/dotnetagents/DotNetAgents",
            DocumentationUrl = "https://github.com/dotnetagents/DotNetAgents/docs/guides/behavior-trees.md"
        };
    }

    /// <inheritdoc />
    protected override Task OnInitializeAsync(IPluginContext context, CancellationToken cancellationToken)
    {
        Logger?.LogInformation(
            "Behavior Trees plugin initialized. Behavior trees can now be created and used.");

        return Task.CompletedTask;
    }
}
