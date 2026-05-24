// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using DotNetAgents.Abstractions.Caching;
using DotNetAgents.Abstractions.Tools;

namespace DotNetAgents.Core.Tools.BuiltIn;

/// <summary>
/// A tool for storing and retrieving key-value data in memory.
/// </summary>
public class MemoryStorageTool : ITool
{
    private readonly ICache _cache;
    private static readonly JsonElement _inputSchema;

    static MemoryStorageTool()
    {
        _inputSchema = JsonSerializer.Deserialize<JsonElement>(@"{
            ""type"": ""object"",
            ""properties"": {
                ""operation"": {
                    ""type"": ""string"",
                    ""description"": ""Operation to perform: 'get', 'set', 'delete', 'list', 'clear'"",
                    ""enum"": [""get"", ""set"", ""delete"", ""list"", ""clear""]
                },
                ""key"": {
                    ""type"": ""string"",
                    ""description"": ""The key for get/set/delete operations""
                },
                ""value"": {
                    ""type"": ""string"",
                    ""description"": ""The value for set operation""
                }
            },
            ""required"": [""operation""]
        }");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryStorageTool"/> class.
    /// </summary>
    /// <param name="cache">The cache to use for storage.</param>
    public MemoryStorageTool(ICache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <inheritdoc/>
    public string Name => "memory_storage";

    /// <inheritdoc/>
    public string Description => "Stores and retrieves key-value data in memory. Supports get, set, delete, list, and clear operations.";

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

        try
        {
            return operation.ToUpperInvariant() switch
            {
                "GET" => await GetAsync(parameters, cancellationToken).ConfigureAwait(false),
                "SET" => await SetAsync(parameters, cancellationToken).ConfigureAwait(false),
                "DELETE" => await DeleteAsync(parameters, cancellationToken).ConfigureAwait(false),
                "LIST" => await ListAsync(cancellationToken).ConfigureAwait(false),
                "CLEAR" => await ClearAsync(cancellationToken).ConfigureAwait(false),
                _ => ToolResult.Failure($"Unknown operation: {operation}")
            };
        }
        catch (Exception ex)
        {
            return ToolResult.Failure(
                $"Operation failed: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["operation"] = operation
                });
        }
    }

    private async Task<ToolResult> GetAsync(IDictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        if (!parameters.TryGetValue("key", out var keyObj) || keyObj is not string key)
        {
            return ToolResult.Failure("Missing 'key' parameter for get operation.");
        }

        var value = await _cache.GetAsync<string>(key, cancellationToken).ConfigureAwait(false);

        if (value == null)
        {
            return ToolResult.Success(
                null,
                new Dictionary<string, object>
                {
                    ["key"] = key,
                    ["found"] = false
                });
        }

        return ToolResult.Success(
            value,
            new Dictionary<string, object>
            {
                ["key"] = key,
                ["found"] = true
            });
    }

    private async Task<ToolResult> SetAsync(IDictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        if (!parameters.TryGetValue("key", out var keyObj) || keyObj is not string key)
        {
            return ToolResult.Failure("Missing 'key' parameter for set operation.");
        }

        if (!parameters.TryGetValue("value", out var valueObj) || valueObj is not string value)
        {
            return ToolResult.Failure("Missing 'value' parameter for set operation.");
        }

        await _cache.SetAsync(key, value, null, cancellationToken).ConfigureAwait(false);

        return ToolResult.Success(
            "Value stored successfully",
            new Dictionary<string, object>
            {
                ["key"] = key
            });
    }

    private async Task<ToolResult> DeleteAsync(IDictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        if (!parameters.TryGetValue("key", out var keyObj) || keyObj is not string key)
        {
            return ToolResult.Failure("Missing 'key' parameter for delete operation.");
        }

        var deleted = await _cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);

        return ToolResult.Success(
            deleted ? "Key deleted successfully" : "Key not found",
            new Dictionary<string, object>
            {
                ["key"] = key,
                ["deleted"] = deleted
            });
    }

    private Task<ToolResult> ListAsync(CancellationToken cancellationToken)
    {
        // ICache doesn't have ListKeys, so we'll return a message
        return Task.FromResult(ToolResult.Success(
            "Key listing is not supported by the underlying cache implementation.",
            new Dictionary<string, object>
            {
                ["note"] = "Use a cache implementation that supports key enumeration for list functionality"
            }));
    }

    private async Task<ToolResult> ClearAsync(CancellationToken cancellationToken)
    {
        await _cache.ClearAsync(cancellationToken).ConfigureAwait(false);

        return ToolResult.Success(
            "Cache cleared successfully",
            new Dictionary<string, object>());
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
