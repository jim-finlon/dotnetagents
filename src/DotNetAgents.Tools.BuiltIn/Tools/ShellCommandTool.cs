using System.Diagnostics;
using System.Text.Json;
using DotNetAgents.Abstractions.Tools;

using DotNetAgents.Abstractions.Exceptions;

namespace DotNetAgents.Tools.BuiltIn;

/// <summary>
/// A tool for executing shell commands with security restrictions.
/// Note: Use with extreme caution in production environments.
/// </summary>
public class ShellCommandTool : ITool
{
    private readonly HashSet<string> _allowedCommands;
    private readonly string? _workingDirectory;
    private readonly int _defaultTimeoutSeconds;
    private static readonly JsonElement _inputSchema;

    static ShellCommandTool()
    {
        _inputSchema = JsonSerializer.Deserialize<JsonElement>(@"{
            ""type"": ""object"",
            ""properties"": {
                ""command"": {
                    ""type"": ""string"",
                    ""description"": ""The shell command to execute""
                },
                ""arguments"": {
                    ""type"": ""string"",
                    ""description"": ""Optional command arguments""
                },
                ""timeout"": {
                    ""type"": ""integer"",
                    ""description"": ""Command timeout in seconds. Default: 30""
                },
                ""working_directory"": {
                    ""type"": ""string"",
                    ""description"": ""Optional working directory for the command""
                }
            },
            ""required"": [""command""]
        }");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellCommandTool"/> class.
    /// </summary>
    /// <param name="allowedCommands">Optional set of allowed command names. If null, all commands are allowed (not recommended).</param>
    /// <param name="workingDirectory">Optional default working directory.</param>
    /// <param name="defaultTimeoutSeconds">Default timeout in seconds. Default: 30.</param>
    public ShellCommandTool(
        HashSet<string>? allowedCommands = null,
        string? workingDirectory = null,
        int defaultTimeoutSeconds = 30)
    {
        _allowedCommands = allowedCommands ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _workingDirectory = workingDirectory;
        _defaultTimeoutSeconds = defaultTimeoutSeconds;
    }

    /// <inheritdoc/>
    public string Name => "shell_command";

    /// <inheritdoc/>
    public string Description => "Executes shell commands with security restrictions. Supports allowlist of commands and configurable timeouts.";

    /// <inheritdoc/>
    public JsonElement InputSchema => _inputSchema;

    /// <inheritdoc/>
    public async Task<ToolResult> ExecuteAsync(
        object input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var parameters = ParseInput(input);
        if (!parameters.TryGetValue("command", out var commandObj) || commandObj is not string command)
        {
            return ToolResult.Failure("Missing or invalid 'command' parameter.");
        }

        if (string.IsNullOrWhiteSpace(command))
        {
            return ToolResult.Failure("Command cannot be null or empty.");
        }

        // Security check: validate command is allowed
        if (_allowedCommands.Count > 0 && !_allowedCommands.Contains(command))
        {
            return ToolResult.Failure(
                $"Command '{command}' is not in the allowed list. Allowed commands: {string.Join(", ", _allowedCommands)}",
                new Dictionary<string, object>
                {
                    ["command"] = command,
                    ["allowed_commands"] = _allowedCommands.ToList()
                });
        }

        var arguments = parameters.TryGetValue("arguments", out var argsObj) && argsObj is string args ? args : "";
        var timeout = _defaultTimeoutSeconds;
        if (parameters.TryGetValue("timeout", out var timeoutObj))
        {
            if (timeoutObj is int timeoutInt && timeoutInt > 0)
            {
                timeout = timeoutInt;
            }
            else if (timeoutObj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Number)
            {
                var timeoutValue = jsonElement.GetInt32();
                if (timeoutValue > 0)
                {
                    timeout = timeoutValue;
                }
            }
        }

        var workingDir = _workingDirectory;
        if (parameters.TryGetValue("working_directory", out var wdObj) && wdObj is string wd)
        {
            workingDir = wd;
        }

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDir ?? Directory.GetCurrentDirectory()
            };

            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder = new System.Text.StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeout));

            var completed = await Task.Run(async () =>
            {
                await Task.Delay(timeout * 1000, cts.Token).ConfigureAwait(false);
                return process.HasExited;
            }, cts.Token).ConfigureAwait(false);

            if (!process.HasExited)
            {
                completed = false;
            }

            if (!completed)
            {
                try
                {
                    process.Kill();
                }
                catch
                {
                    // Ignore kill errors
                }

                return ToolResult.Failure(
                    $"Command timed out after {timeout} seconds.",
                    new Dictionary<string, object>
                    {
                        ["command"] = command,
                        ["timeout"] = timeout
                    });
            }

            var output = outputBuilder.ToString().TrimEnd();
            var error = errorBuilder.ToString().TrimEnd();
            var exitCode = process.ExitCode;

            var result = new Dictionary<string, object>
            {
                ["exit_code"] = exitCode,
                ["stdout"] = output,
                ["stderr"] = error,
                ["success"] = exitCode == 0
            };

            return ToolResult.Success(
                JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }),
                new Dictionary<string, object>
                {
                    ["command"] = command,
                    ["exit_code"] = exitCode,
                    ["success"] = exitCode == 0
                });
        }
        catch (OperationCanceledException)
        {
            return ToolResult.Failure(
                $"Command execution was cancelled or timed out.",
                new Dictionary<string, object>
                {
                    ["command"] = command
                });
        }
        catch (Exception ex)
        {
            return ToolResult.Failure(
                $"Command execution failed: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["command"] = command
                });
        }
    }

    private static IDictionary<string, object> ParseInput(object input)
    {
        if (input is JsonElement jsonElement)
        {
            var dict = new Dictionary<string, object>();
            foreach (var prop in jsonElement.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString()!,
                    JsonValueKind.Number => prop.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => prop.Value.ToString()
                };
            }
            return dict;
        }

        if (input is IDictionary<string, object> dictInput)
        {
            return dictInput;
        }

        throw new ArgumentException("Input must be JsonElement or IDictionary<string, object>", nameof(input));
    }
}
