// SPDX-License-Identifier: Apache-2.0

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DotNetAgents.Abstractions.Tools;

using DotNetAgents.Abstractions.Exceptions;

namespace DotNetAgents.Tools.BuiltIn;

/// <summary>
/// A tool for generating cryptographic hashes (MD5, SHA256, SHA512, etc.).
/// </summary>
public class HashGeneratorTool : ITool
{
    private static readonly JsonElement _inputSchema;

    static HashGeneratorTool()
    {
        _inputSchema = JsonSerializer.Deserialize<JsonElement>(@"{
            ""type"": ""object"",
            ""properties"": {
                ""algorithm"": {
                    ""type"": ""string"",
                    ""description"": ""Hash algorithm: 'md5', 'sha1', 'sha256', 'sha384', 'sha512'. Default: 'sha256'"",
                    ""enum"": [""md5"", ""sha1"", ""sha256"", ""sha384"", ""sha512""]
                },
                ""input"": {
                    ""type"": ""string"",
                    ""description"": ""The input text to hash""
                },
                ""format"": {
                    ""type"": ""string"",
                    ""description"": ""Output format: 'hex' or 'base64'. Default: 'hex'"",
                    ""enum"": [""hex"", ""base64""]
                }
            },
            ""required"": [""input""]
        }");
    }

    /// <inheritdoc/>
    public string Name => "hash_generator";

    /// <inheritdoc/>
    public string Description => "Generates cryptographic hashes (MD5, SHA1, SHA256, SHA384, SHA512) from input text. Supports hex and base64 output formats.";

    /// <inheritdoc/>
    public JsonElement InputSchema => _inputSchema;

    /// <inheritdoc/>
    public Task<ToolResult> ExecuteAsync(
        object input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var parameters = ParseInput(input);
        if (!parameters.TryGetValue("input", out var inputObj) || inputObj is not string inputText)
        {
            return Task.FromResult(ToolResult.Failure("Missing or invalid 'input' parameter."));
        }

        if (string.IsNullOrWhiteSpace(inputText))
        {
            return Task.FromResult(ToolResult.Failure("Input cannot be null or empty."));
        }

        var algorithm = "sha256";
        if (parameters.TryGetValue("algorithm", out var algoObj) && algoObj is string algoStr)
        {
            algorithm = algoStr.ToLowerInvariant();
        }

        var format = "hex";
        if (parameters.TryGetValue("format", out var formatObj) && formatObj is string formatStr)
        {
            format = formatStr.ToLowerInvariant();
        }

        try
        {
            var hashBytes = ComputeHash(inputText, algorithm);
            var hashString = format == "base64"
                ? Convert.ToBase64String(hashBytes)
                : Convert.ToHexString(hashBytes).ToLowerInvariant();

            return Task.FromResult(ToolResult.Success(
                hashString,
                new Dictionary<string, object>
                {
                    ["algorithm"] = algorithm,
                    ["format"] = format,
                    ["input_length"] = inputText.Length
                }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Failure(
                $"Hash generation failed: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["algorithm"] = algorithm
                }));
        }
    }

    private static byte[] ComputeHash(string input, string algorithm)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);

        return algorithm.ToLowerInvariant() switch
        {
            "md5" => MD5.HashData(inputBytes),
            "sha1" => SHA1.HashData(inputBytes),
            "sha256" => SHA256.HashData(inputBytes),
            "sha384" => SHA384.HashData(inputBytes),
            "sha512" => SHA512.HashData(inputBytes),
            _ => throw new ArgumentException($"Unsupported hash algorithm: {algorithm}")
        };
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
