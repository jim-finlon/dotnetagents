// SPDX-License-Identifier: Apache-2.0

using System.CommandLine;
using DotNetAgents.CLI.Scaffolding;

namespace DotNetAgents.CLI.Commands;

/// <summary>
/// Command for creating new DotNetAgents projects.
/// </summary>
public static class NewCommand
{
    public static Command Create()
    {
        var command = new Command("new", "Create a new DotNetAgents project");

        var projectNameArgument = new Argument<string>(
            "name",
            "Name of the project to create");

        var templateOption = new Option<string>(
            "--template",
            description: "Project template (agent, chain, workflow, rag, multi-agent)")
        {
            IsRequired = false
        };
        templateOption.AddAlias("-t");

        command.AddArgument(projectNameArgument);
        command.AddOption(templateOption);

        command.SetHandler(async (name, template) =>
        {
            await HandleNewProjectAsync(name, template ?? "agent");
        }, projectNameArgument, templateOption);

        return command;
    }

    private static async Task HandleNewProjectAsync(string projectName, string template)
    {
        Console.WriteLine($"Creating new DotNetAgents project: {projectName}");
        Console.WriteLine($"Template: {template}");

        var result = await new DotNetAgentsScaffolder().CreateProjectAsync(projectName, template).ConfigureAwait(false);

        Console.WriteLine($"Project '{projectName}' created successfully!");
        Console.WriteLine($"Created files:");
        foreach (var file in result.CreatedFiles)
        {
            Console.WriteLine($"  {Path.GetRelativePath(result.RootPath, file)}");
        }

        Console.WriteLine($"Next steps:");
        Console.WriteLine($"  cd {projectName}");
        Console.WriteLine($"  dotnet restore");
        Console.WriteLine($"  dotnet build");
    }
}
