using System.CommandLine;

namespace DotNetAgents.CLI.Commands;

/// <summary>
/// Command for validating DotNetAgents project configuration.
/// </summary>
public static class ValidateCommand
{
    public static Command Create()
    {
        var command = new Command("validate", "Validate DotNetAgents project configuration");

        var projectOption = new Option<string>(
            "--project",
            description: "Path to project file or directory")
        {
            IsRequired = false
        };
        projectOption.AddAlias("-p");

        command.AddOption(projectOption);

        command.SetHandler(async (projectPath) =>
        {
            await HandleValidateAsync(projectPath ?? Directory.GetCurrentDirectory());
        }, projectOption);

        return command;
    }

    private static async Task HandleValidateAsync(string projectPath)
    {
        Console.WriteLine($"Validating DotNetAgents project: {projectPath}");
        Console.WriteLine();

        var issues = new List<string>();
        var warnings = new List<string>();

        // Check for .csproj file
        var csprojFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.TopDirectoryOnly);
        if (csprojFiles.Length == 0)
        {
            issues.Add("No .csproj file found");
        }
        else
        {
            Console.WriteLine($"✓ Found {csprojFiles.Length} project file(s)");

            // Check for DotNetAgents packages
            foreach (var csprojFile in csprojFiles)
            {
                var content = await File.ReadAllTextAsync(csprojFile);
                if (!content.Contains("DotNetAgents"))
                {
                    warnings.Add($"Project {Path.GetFileName(csprojFile)} may not reference DotNetAgents packages");
                }
                else
                {
                    Console.WriteLine($"✓ {Path.GetFileName(csprojFile)} references DotNetAgents packages");
                }
            }
        }

        // Check for configuration files
        var configFiles = new[] { "appsettings.json", "appsettings.Development.json", ".env" };
        var foundConfigs = new List<string>();

        foreach (var configFile in configFiles)
        {
            var configPath = Path.Combine(projectPath, configFile);
            if (File.Exists(configPath))
            {
                foundConfigs.Add(configFile);
                Console.WriteLine($"✓ Found configuration file: {configFile}");
            }
        }

        if (foundConfigs.Count == 0)
        {
            warnings.Add("No configuration files found (appsettings.json, .env, etc.)");
        }

        // Check for .env file specifically
        var envPath = Path.Combine(projectPath, ".env");
        if (File.Exists(envPath))
        {
            var envContent = await File.ReadAllTextAsync(envPath);
            if (envContent.Contains("CONNECTIONSTRINGS__DEFAULTCONNECTION") ||
                envContent.Contains("OPENAI_API_KEY"))
            {
                Console.WriteLine("✓ .env file contains expected configuration");
            }
            else
            {
                warnings.Add(".env file exists but may not contain required configuration");
            }
        }

        // Summary
        Console.WriteLine();
        if (issues.Count == 0 && warnings.Count == 0)
        {
            Console.WriteLine("✓ Project validation passed!");
            Console.WriteLine();
            Console.WriteLine("Next steps:");
            Console.WriteLine("  - Ensure API keys are configured in .env or appsettings.json");
            Console.WriteLine("  - Run 'dotnet agents test' to verify setup");
        }
        else
        {
            if (issues.Count > 0)
            {
                Console.WriteLine("✗ Validation issues found:");
                foreach (var issue in issues)
                {
                    Console.WriteLine($"  ✗ {issue}");
                }
            }

            if (warnings.Count > 0)
            {
                Console.WriteLine("⚠ Warnings:");
                foreach (var warning in warnings)
                {
                    Console.WriteLine($"  ⚠ {warning}");
                }
            }
        }

        await Task.CompletedTask;
    }
}
