using System.CommandLine;
using System.Diagnostics;

namespace DotNetAgents.CLI.Commands;

/// <summary>
/// Command for running tests with test infrastructure setup.
/// </summary>
public static class TestCommand
{
    public static Command Create()
    {
        var command = new Command("test", "Run tests with test infrastructure setup");

        var startInfrastructureOption = new Option<bool>(
            "--start-infrastructure",
            description: "Start docker-compose test infrastructure before running tests")
        {
            IsRequired = false
        };
        startInfrastructureOption.AddAlias("-s");

        var stopInfrastructureOption = new Option<bool>(
            "--stop-infrastructure",
            description: "Stop docker-compose test infrastructure after tests")
        {
            IsRequired = false
        };

        command.AddOption(startInfrastructureOption);
        command.AddOption(stopInfrastructureOption);

        command.SetHandler(async (startInfra, stopInfra) =>
        {
            await HandleTestAsync(startInfra, stopInfra);
        }, startInfrastructureOption, stopInfrastructureOption);

        return command;
    }

    private static async Task HandleTestAsync(bool startInfrastructure, bool stopInfrastructure)
    {
        var projectRoot = FindProjectRoot();
        var dockerComposePath = Path.Combine(projectRoot, "docker", "docker-compose.test.yml");

        if (startInfrastructure)
        {
            Console.WriteLine("Starting test infrastructure...");
            await StartDockerComposeAsync(dockerComposePath);
            Console.WriteLine("Waiting for services to be healthy...");
            await Task.Delay(TimeSpan.FromSeconds(10));
        }

        Console.WriteLine("Running tests...");
        await RunTestsAsync();

        if (stopInfrastructure)
        {
            Console.WriteLine("Stopping test infrastructure...");
            await StopDockerComposeAsync(dockerComposePath);
        }
    }

    private static string FindProjectRoot()
    {
        var current = Directory.GetCurrentDirectory();
        while (current != null && !File.Exists(Path.Combine(current, "DotNetAgents.sln")))
        {
            current = Directory.GetParent(current)?.FullName;
        }
        return current ?? Directory.GetCurrentDirectory();
    }

    private static async Task StartDockerComposeAsync(string composePath)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker-compose",
                Arguments = $"-f \"{composePath}\" up -d",
                WorkingDirectory = Path.GetDirectoryName(composePath),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"Failed to start test infrastructure: {error}");
        }
    }

    private static async Task StopDockerComposeAsync(string composePath)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker-compose",
                Arguments = $"-f \"{composePath}\" down",
                WorkingDirectory = Path.GetDirectoryName(composePath),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        await process.WaitForExitAsync();
    }

    private static async Task RunTestsAsync()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "test",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Console.WriteLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Console.Error.WriteLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        Environment.ExitCode = process.ExitCode;
    }
}
