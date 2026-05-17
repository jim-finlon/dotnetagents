using System.CommandLine;
using DotNetAgents.Core.Chains;
using DotNetAgents.Core.Prompts;
using DotNetAgents.Workflow.Graph;
using DotNetAgents.Workflow.Execution;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.REPL;

/// <summary>
/// Interactive REPL for testing DotNetAgents chains and workflows.
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("DotNetAgents REPL - Interactive testing environment for chains and workflows")
        {
            Name = "dotnet-agents-repl"
        };

        var interactiveCommand = new Command("interactive", "Start interactive REPL session");
        rootCommand.AddCommand(interactiveCommand);

        var chainCommand = new Command("chain", "Test a chain interactively");
        var promptArg = new Argument<string>("prompt", "The prompt template");
        var inputArg = new Argument<string>("input", "The input to the chain");
        chainCommand.AddArgument(promptArg);
        chainCommand.AddArgument(inputArg);
        rootCommand.AddCommand(chainCommand);

        interactiveCommand.SetHandler(async () =>
        {
            await RunInteractiveSessionAsync();
        });

        chainCommand.SetHandler(async (string prompt, string input) =>
        {
            await TestChainAsync(prompt, input);
        }, promptArg, inputArg);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task RunInteractiveSessionAsync()
    {
        Console.WriteLine("DotNetAgents REPL");
        Console.WriteLine("================");
        Console.WriteLine("Type 'help' for commands, 'exit' to quit");
        Console.WriteLine();

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<Program>();

        while (true)
        {
            Console.Write("dotnet-agents> ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0].ToLowerInvariant();
            var args = parts.Length > 1 ? parts[1] : string.Empty;

            try
            {
                switch (command)
                {
                    case "exit":
                    case "quit":
                        Console.WriteLine("Goodbye!");
                        return;

                    case "help":
                        ShowHelp();
                        break;

                    case "chain":
                        await HandleChainCommandAsync(args, logger);
                        break;

                    case "workflow":
                        await HandleWorkflowCommandAsync(args, logger);
                        break;

                    case "prompt":
                        await HandlePromptCommandAsync(args);
                        break;

                    default:
                        Console.WriteLine($"Unknown command: {command}. Type 'help' for available commands.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (logger != null)
                {
                    logger.LogError(ex, "Error executing command");
                }
            }

            Console.WriteLine();
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Available commands:");
        Console.WriteLine("  chain <prompt> <input>  - Test a chain with prompt template and input");
        Console.WriteLine("  workflow <name>         - Execute a workflow (interactive)");
        Console.WriteLine("  prompt <template> <vars> - Format a prompt template");
        Console.WriteLine("  help                    - Show this help message");
        Console.WriteLine("  exit                    - Exit the REPL");
    }

    private static async Task HandleChainCommandAsync(string args, ILogger logger)
    {
        var parts = args.Split('|', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            Console.WriteLine("Usage: chain <prompt-template>|<input>");
            Console.WriteLine("Example: chain \"Hello {name}\"|World");
            return;
        }

        var promptTemplate = parts[0].Trim();
        var input = parts[1].Trim();

        Console.WriteLine($"Prompt Template: {promptTemplate}");
        Console.WriteLine($"Input: {input}");

        try
        {
            var template = new PromptTemplate(promptTemplate);
            var formatted = await template.FormatAsync(new Dictionary<string, object> { ["input"] = input });

            Console.WriteLine($"Formatted Prompt: {formatted}");
            Console.WriteLine();
            Console.WriteLine("Note: To execute with an actual LLM, configure a provider:");
            Console.WriteLine("  - Add DotNetAgents.Providers.OpenAI package");
            Console.WriteLine("  - Configure API key");
            Console.WriteLine("  - Use ChainBuilder.WithLLM()");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static async Task HandleWorkflowCommandAsync(string args, ILogger logger)
    {
        Console.WriteLine("Workflow execution:");
        Console.WriteLine("Creating a simple workflow...");

        try
        {
            var workflowState = new Dictionary<string, object>
            {
                ["input"] = args,
                ["step"] = 0
            };

            var workflow = new StateGraph<Dictionary<string, object>>()
                .AddNode("start", async (state, ct) =>
                {
                    state["step"] = 1;
                    state["message"] = $"Processing: {state["input"]}";
                    Console.WriteLine($"  [Node: start] {state["message"]}");
                    return state;
                })
                .AddNode("process", async (state, ct) =>
                {
                    state["step"] = 2;
                    state["processed"] = true;
                    Console.WriteLine($"  [Node: process] Step {state["step"]}");
                    return state;
                })
                .AddNode("end", async (state, ct) =>
                {
                    state["step"] = 3;
                    state["completed"] = true;
                    Console.WriteLine($"  [Node: end] Completed");
                    return state;
                })
                .AddEdge("start", "process")
                .AddEdge("process", "end")
                .SetEntryPoint("start")
                .AddExitPoint("end");

            workflow.Validate();

            var executor = new GraphExecutor<Dictionary<string, object>>(workflow);
            var result = await executor.ExecuteAsync(workflowState);

            Console.WriteLine();
            Console.WriteLine("Final State:");
            foreach (var kvp in result)
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static async Task HandlePromptCommandAsync(string args)
    {
        var parts = args.Split('|', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            Console.WriteLine("Usage: prompt <template>|<variables>");
            Console.WriteLine("Example: prompt \"Hello {name}, today is {day}\"|name=Alice,day=Monday");
            return;
        }

        var template = parts[0].Trim();
        var varsString = parts[1].Trim();

        try
        {
            var variables = new Dictionary<string, object>();
            var varParts = varsString.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var varPart in varParts)
            {
                var kvp = varPart.Split('=', 2);
                if (kvp.Length == 2)
                {
                    variables[kvp[0].Trim()] = kvp[1].Trim();
                }
            }

            var promptTemplate = new PromptTemplate(template);
            var result = await promptTemplate.FormatAsync(variables);

            Console.WriteLine($"Template: {template}");
            Console.WriteLine($"Variables: {string.Join(", ", variables.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
            Console.WriteLine($"Result: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static async Task TestChainAsync(string prompt, string input)
    {
        Console.WriteLine($"Testing chain with prompt: {prompt}");
        Console.WriteLine($"Input: {input}");

        try
        {
            var template = new PromptTemplate(prompt);
            var formatted = await template.FormatAsync(new Dictionary<string, object> { ["input"] = input });

            Console.WriteLine($"Formatted: {formatted}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
