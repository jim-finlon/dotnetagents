// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Ecosystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.StateMachines;

/// <summary>
/// Plugin for State Machines functionality.
/// </summary>
public class StateMachinesPlugin : PluginBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StateMachinesPlugin"/> class.
    /// </summary>
    public StateMachinesPlugin()
    {
        Metadata = new PluginMetadata
        {
            Id = "statemachines",
            Name = "State Machines",
            Version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0",
            Description = "Provides state machine functionality for managing agent lifecycle and operational states. Supports hierarchical, parallel, and timed state machines with integration into workflows, worker pools, and message buses.",
            Author = "DotNetAgents",
            License = "MIT",
            Category = "Agent Capabilities",
            Tags = new List<string> { "state-machine", "lifecycle", "workflow", "agents" },
            Dependencies = new List<string>(),
            RepositoryUrl = "https://github.com/dotnetagents/DotNetAgents",
            DocumentationUrl = "https://github.com/dotnetagents/DotNetAgents/docs/guides/state-machines.md"
        };
    }

    /// <inheritdoc />
    protected override Task OnInitializeAsync(IPluginContext context, CancellationToken cancellationToken)
    {
        // State machines are typically created on-demand by users, so we don't need to register
        // default services here. However, if needed, we could register common factories or helpers.

        Logger?.LogInformation(
            "State Machines plugin initialized. State machines can now be created and used.");

        return Task.CompletedTask;
    }
}
