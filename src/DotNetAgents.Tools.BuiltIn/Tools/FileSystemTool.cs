using System.Text.Json;
using DotNetAgents.Abstractions.Exceptions;
using DotNetAgents.Abstractions.Tools;


namespace DotNetAgents.Tools.BuiltIn;

/// <summary>
/// A tool for basic file system operations (read, write, list directory).
/// Note: This tool should be used with caution in production environments.
/// </summary>
public class FileSystemTool : ITool
{
    private readonly string? _allowedBasePath;
    private static readonly JsonElement _inputSchema;

    static FileSystemTool()
    {
        _inputSchema = JsonSerializer.Deserialize<JsonElement>(@"{
            ""type"": ""object"",
            ""properties"": {
                ""operation"": {
                    ""type"": ""string"",
                    ""description"": ""The operation to perform: 'read', 'write', 'list', or 'exists'""
                },
                ""path"": {
                    ""type"": ""string"",
                    ""description"": ""The file or directory path""
                },
                ""content"": {
                    ""type"": ""string"",
                    ""description"": ""Content to write (required for 'write' operation)""
                }
            },
            ""required"": [""operation"", ""path""]
        }");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemTool"/> class.
    /// </summary>
    /// <param name="allowedBasePath">Optional base path to restrict file operations to. If null, operations are unrestricted.</param>
    public FileSystemTool(string? allowedBasePath = null)
    {
        _allowedBasePath = allowedBasePath;
    }

    /// <inheritdoc/>
    public string Name => "file_system";

    /// <inheritdoc/>
    public string Description => "Performs file system operations: read file, write file, list directory. Use with caution.";

    /// <inheritdoc/>
    public JsonElement InputSchema => _inputSchema;

    /// <inheritdoc/>
    public async Task<ToolResult> ExecuteAsync(
        object input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var parameters = ParseInput(input);
        if (!parameters.TryGetValue("operation", out var operationObj) || operationObj is not string operation)
        {
            return ToolResult.Failure("Missing or invalid 'operation' parameter.");
        }

        if (!parameters.TryGetValue("path", out var pathObj) || pathObj is not string path)
        {
            return ToolResult.Failure("Missing or invalid 'path' parameter.");
        }

        // Validate and sanitize path
        var safePath = ValidateAndSanitizePath(path);

        try
        {
            return operation.ToLowerInvariant() switch
            {
                "read" => await ReadFileAsync(safePath, cancellationToken).ConfigureAwait(false),
                "write" => await WriteFileAsync(safePath, parameters, cancellationToken).ConfigureAwait(false),
                "list" => await ListDirectoryAsync(safePath, cancellationToken).ConfigureAwait(false),
                "exists" => await CheckExistsAsync(safePath, cancellationToken).ConfigureAwait(false),
                _ => ToolResult.Failure($"Unknown operation: {operation}. Supported operations: read, write, list, exists")
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            return ToolResult.Failure(
                $"Access denied: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["operation"] = operation,
                    ["path"] = safePath
                });
        }
        catch (Exception ex)
        {
            return ToolResult.Failure(
                $"File system operation failed: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["operation"] = operation,
                    ["path"] = safePath
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

    private string ValidateAndSanitizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));
        }

        // Resolve relative paths
        var fullPath = Path.IsPathRooted(path) ? path : Path.GetFullPath(path);

        // If base path is restricted, ensure the path is within it
        if (!string.IsNullOrEmpty(_allowedBasePath))
        {
            var basePath = Path.GetFullPath(_allowedBasePath);
            if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException($"Path must be within allowed base path: {_allowedBasePath}");
            }
        }

        // Prevent directory traversal attacks
        if (path.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("Path cannot contain '..' (directory traversal).");
        }

        return fullPath;
    }

    private async Task<ToolResult> ReadFileAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return ToolResult.Failure(
                $"File not found: {path}",
                new Dictionary<string, object>
                {
                    ["path"] = path
                });
        }

        var content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        var fileInfo = new FileInfo(path);

        return ToolResult.Success(
            content,
            new Dictionary<string, object>
            {
                ["path"] = path,
                ["size"] = fileInfo.Length,
                ["modified"] = fileInfo.LastWriteTimeUtc.ToString("O")
            });
    }

    private async Task<ToolResult> WriteFileAsync(
        string path,
        IDictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        if (!parameters.TryGetValue("content", out var contentObj) || contentObj is not string content)
        {
            return ToolResult.Failure("Missing 'content' parameter for write operation");
        }

        // Ensure directory exists
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, content, cancellationToken).ConfigureAwait(false);

        return ToolResult.Success(
            $"File written successfully: {path}",
            new Dictionary<string, object>
            {
                ["path"] = path,
                ["size"] = content.Length
            });
    }

    private Task<ToolResult> ListDirectoryAsync(string path, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(path))
        {
            return Task.FromResult(ToolResult.Failure(
                $"Directory not found: {path}",
                new Dictionary<string, object>
                {
                    ["path"] = path
                }));
        }

        var files = Directory.GetFiles(path).Select(f => new
        {
            name = Path.GetFileName(f),
            type = "file",
            path = f
        });

        var directories = Directory.GetDirectories(path).Select(d => new
        {
            name = Path.GetFileName(d),
            type = "directory",
            path = d
        });

        var items = files.Concat(directories).ToList();

        return Task.FromResult(ToolResult.Success(
            System.Text.Json.JsonSerializer.Serialize(items, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
            new Dictionary<string, object>
            {
                ["path"] = path,
                ["item_count"] = items.Count
            }));
    }

    private Task<ToolResult> CheckExistsAsync(string path, CancellationToken cancellationToken)
    {
        var exists = File.Exists(path) || Directory.Exists(path);

        return Task.FromResult(ToolResult.Success(
            exists ? "exists" : "not_found",
            new Dictionary<string, object>
            {
                ["path"] = path,
                ["exists"] = exists
            }));
    }
}
