// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using System.Text.Json;
using DotNetAgents.Abstractions.Tools;

using DotNetAgents.Abstractions.Exceptions;

namespace DotNetAgents.Tools.BuiltIn;

/// <summary>
/// A tool for reading and parsing CSV files.
/// </summary>
public class CsvReaderTool : ITool
{
    private static readonly JsonElement _inputSchema;

    static CsvReaderTool()
    {
        _inputSchema = JsonSerializer.Deserialize<JsonElement>(@"{
            ""type"": ""object"",
            ""properties"": {
                ""operation"": {
                    ""type"": ""string"",
                    ""description"": ""Operation: 'read', 'parse', 'to_json'"",
                    ""enum"": [""read"", ""parse"", ""to_json""]
                },
                ""file_path"": {
                    ""type"": ""string"",
                    ""description"": ""Path to CSV file (for 'read' operation)""
                },
                ""csv_data"": {
                    ""type"": ""string"",
                    ""description"": ""CSV data as string (for 'parse'/'to_json' operations)""
                },
                ""delimiter"": {
                    ""type"": ""string"",
                    ""description"": ""CSV delimiter. Default: ','""
                },
                ""has_header"": {
                    ""type"": ""boolean"",
                    ""description"": ""Whether CSV has header row. Default: true""
                }
            },
            ""required"": [""operation""]
        }");
    }

    /// <inheritdoc/>
    public string Name => "csv_reader";

    /// <inheritdoc/>
    public string Description => "Reads and parses CSV files. Supports reading from files, parsing CSV strings, and converting to JSON format.";

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
            return operation.ToLowerInvariant() switch
            {
                "read" => await ReadCsvFileAsync(parameters, cancellationToken).ConfigureAwait(false),
                "parse" => await ParseCsvAsync(parameters, cancellationToken).ConfigureAwait(false),
                "to_json" => await CsvToJsonAsync(parameters, cancellationToken).ConfigureAwait(false),
                _ => ToolResult.Failure($"Unknown operation: {operation}")
            };
        }
        catch (Exception ex)
        {
            return ToolResult.Failure(
                $"CSV operation failed: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["operation"] = operation
                });
        }
    }

    private async Task<ToolResult> ReadCsvFileAsync(IDictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        if (!parameters.TryGetValue("file_path", out var pathObj) || pathObj is not string filePath)
        {
            return ToolResult.Failure("Missing 'file_path' parameter for read operation.");
        }

        if (!File.Exists(filePath))
        {
            return ToolResult.Failure($"CSV file not found: {filePath}");
        }

        var csvData = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
        return await ParseCsvAsync(parameters, csvData, cancellationToken).ConfigureAwait(false);
    }

    private Task<ToolResult> ParseCsvAsync(IDictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        if (!parameters.TryGetValue("csv_data", out var csvDataObj) || csvDataObj is not string csvData)
        {
            return Task.FromResult(ToolResult.Failure("Missing 'csv_data' parameter for parse operation."));
        }

        return ParseCsvAsync(parameters, csvData, cancellationToken);
    }

    private Task<ToolResult> CsvToJsonAsync(IDictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        if (!parameters.TryGetValue("csv_data", out var csvDataObj) || csvDataObj is not string csvData)
        {
            return Task.FromResult(ToolResult.Failure("Missing 'csv_data' parameter for to_json operation."));
        }

        return ParseCsvAsync(parameters, csvData, cancellationToken);
    }

    private Task<ToolResult> ParseCsvAsync(IDictionary<string, object> parameters, string csvData, CancellationToken cancellationToken)
    {
        var delimiter = parameters.TryGetValue("delimiter", out var delimObj) && delimObj is string delim ? delim : ",";
        var hasHeader = true;
        if (parameters.TryGetValue("has_header", out var headerObj))
        {
            hasHeader = headerObj is bool headerBool ? headerBool :
                       headerObj is JsonElement je && je.ValueKind == JsonValueKind.True;
        }

        var lines = csvData.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            return Task.FromResult(ToolResult.Success(
                "[]",
                new Dictionary<string, object>
                {
                    ["row_count"] = 0
                }));
        }

        var headers = hasHeader ? ParseCsvLine(lines[0], delimiter) : null;
        var startIndex = hasHeader ? 1 : 0;

        var rows = new List<Dictionary<string, object>>();
        for (int i = startIndex; i < lines.Length; i++)
        {
            var values = ParseCsvLine(lines[i], delimiter);
            var row = new Dictionary<string, object>();

            if (headers != null)
            {
                for (int j = 0; j < Math.Min(headers.Count, values.Count); j++)
                {
                    row[headers[j]] = values[j];
                }
            }
            else
            {
                for (int j = 0; j < values.Count; j++)
                {
                    row[$"column{j + 1}"] = values[j];
                }
            }

            rows.Add(row);
        }

        return Task.FromResult(ToolResult.Success(
            JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }),
            new Dictionary<string, object>
            {
                ["row_count"] = rows.Count,
                ["column_count"] = headers?.Count ?? (rows.Count > 0 ? rows[0].Count : 0)
            }));
    }

    private static List<string> ParseCsvLine(string line, string delimiter)
    {
        var values = new List<string>();
        var currentValue = new System.Text.StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Escaped quote
                    currentValue.Append('"');
                    i++;
                }
                else
                {
                    // Toggle quote state
                    inQuotes = !inQuotes;
                }
            }
            else if (c.ToString() == delimiter && !inQuotes)
            {
                values.Add(currentValue.ToString());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(c);
            }
        }

        values.Add(currentValue.ToString());
        return values;
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
