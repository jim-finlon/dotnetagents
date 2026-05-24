// SPDX-License-Identifier: Apache-2.0

using System.CommandLine;
using DotNetAgents.CLI.Scaffolding;

namespace DotNetAgents.CLI.Commands;

/// <summary>
/// Command for adding components to existing projects.
/// </summary>
public static class AddCommand
{
    public static Command Create()
    {
        var command = new Command("add", "Add components to your DotNetAgents project");

        var componentArgument = new Argument<string>(
            "component",
            "Component to add (chain, workflow, agent, tool)");

        var nameOption = new Option<string>(
            "--name",
            description: "Name for the component")
        {
            IsRequired = false
        };
        nameOption.AddAlias("-n");

        command.AddArgument(componentArgument);
        command.AddOption(nameOption);

        command.SetHandler(async (component, name) =>
        {
            await HandleAddComponentAsync(component, name);
        }, componentArgument, nameOption);

        return command;
    }

    private static async Task HandleAddComponentAsync(string component, string? name)
    {
        Console.WriteLine($"Adding {component} component...");

        try
        {
            var defaultName = component.ToLowerInvariant() switch
            {
                "chain" => "MyChain",
                "workflow" => "MyWorkflow",
                "agent" => "MyAgent",
                "tool" => "MyTool",
                _ => name ?? "MyComponent"
            };
            var result = await new DotNetAgentsScaffolder()
                .AddComponentAsync(component, name ?? defaultName)
                .ConfigureAwait(false);

            Console.WriteLine("Created files:");
            foreach (var file in result.CreatedFiles)
            {
                Console.WriteLine($"  {Path.GetRelativePath(result.RootPath, file)}");
            }
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine("Available components: chain, workflow, agent, tool");
        }
    }
}
