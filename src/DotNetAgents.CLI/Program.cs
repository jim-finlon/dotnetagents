// SPDX-License-Identifier: Apache-2.0

using System.CommandLine;
using DotNetAgents.CLI.Commands;

namespace DotNetAgents.CLI;

/// <summary>
/// DotNetAgents CLI - Command-line tools for scaffolding and managing DotNetAgents projects.
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("DotNetAgents CLI - Tools for building AI agents, chains, and workflows")
        {
            Name = "dotnet-agents"
        };

               // Add subcommands
               rootCommand.AddCommand(NewCommand.Create());
               rootCommand.AddCommand(AddCommand.Create());
               rootCommand.AddCommand(ValidateCommand.Create());
               rootCommand.AddCommand(TestCommand.Create());
               rootCommand.AddCommand(VisualizeCommand.Create());
               rootCommand.AddCommand(new DbLearnCommand());
               rootCommand.AddCommand(new DbPatternsCommand());

        return await rootCommand.InvokeAsync(args);
    }
}
