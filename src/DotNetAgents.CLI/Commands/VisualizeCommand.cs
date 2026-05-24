// SPDX-License-Identifier: Apache-2.0

using System.CommandLine;
using DotNetAgents.Workflow.Graph;
using DotNetAgents.Workflow.Visualization;

namespace DotNetAgents.CLI.Commands;

/// <summary>
/// Command for visualizing workflows.
/// </summary>
public static class VisualizeCommand
{
    public static Command Create()
    {
        var command = new Command("visualize", "Visualize workflow graphs");

        var formatOption = new Option<string>(
            "--format",
            getDefaultValue: () => "dot",
            description: "Output format: dot, mermaid, or json")
        {
            IsRequired = false
        };
        formatOption.AddAlias("-f");

        var outputOption = new Option<string>(
            "--output",
            description: "Output file path (optional, defaults to stdout)")
        {
            IsRequired = false
        };
        outputOption.AddAlias("-o");

        command.AddOption(formatOption);
        command.AddOption(outputOption);

        command.SetHandler(async (format, output) =>
        {
            await HandleVisualizeAsync(format, output);
        }, formatOption, outputOption);

        return command;
    }

    private static async Task HandleVisualizeAsync(string format, string? outputPath)
    {
        Console.WriteLine("Workflow Visualization");
        Console.WriteLine("=====================");
        Console.WriteLine();
        Console.WriteLine("Note: This command demonstrates workflow visualization.");
        Console.WriteLine("In a full implementation, this would:");
        Console.WriteLine("  1. Parse workflow definitions from code");
        Console.WriteLine("  2. Generate visualization in the requested format");
        Console.WriteLine("  3. Output to file or stdout");
        Console.WriteLine();

        // Example workflow for demonstration
        var workflow = new StateGraph<Dictionary<string, object>>()
            .AddNode("start", async (state, ct) => state)
            .AddNode("process", async (state, ct) => state)
            .AddNode("end", async (state, ct) => state)
            .AddEdge("start", "process")
            .AddEdge("process", "end")
            .SetEntryPoint("start")
            .AddExitPoint("end");

        var visualizationService = new GraphVisualizationService();
        string result;

        switch (format.ToLowerInvariant())
        {
            case "dot":
                result = visualizationService.GenerateDot(workflow);
                break;
            case "mermaid":
                result = visualizationService.GenerateMermaid(workflow);
                break;
            case "json":
                result = visualizationService.GenerateJson(workflow);
                break;
            default:
                Console.WriteLine($"Unknown format: {format}. Supported formats: dot, mermaid, json");
                return;
        }

        if (!string.IsNullOrEmpty(outputPath))
        {
            await File.WriteAllTextAsync(outputPath, result);
            Console.WriteLine($"Visualization written to: {outputPath}");
        }
        else
        {
            Console.WriteLine(result);
        }

        await Task.CompletedTask;
    }
}
