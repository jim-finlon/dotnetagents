// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using DotNetAgents.Abstractions.Exceptions;
using DotNetAgents.Abstractions.Tools;


namespace DotNetAgents.Tools.BuiltIn;

/// <summary>
/// A tool that provides current date and time information.
/// </summary>
public class DateTimeTool : ITool
{
    private static readonly JsonElement _inputSchema;

    static DateTimeTool()
    {
        _inputSchema = JsonSerializer.Deserialize<JsonElement>(@"{
            ""type"": ""object"",
            ""properties"": {
                ""format"": {
                    ""type"": ""string"",
                    ""description"": ""The format to return: 'iso', 'unix', 'readable', or a custom format string (default: 'iso')""
                },
                ""timezone"": {
                    ""type"": ""string"",
                    ""description"": ""Optional timezone identifier (e.g., 'UTC', 'America/New_York', 'Europe/London'). Defaults to UTC.""
                }
            },
            ""required"": []
        }");
    }

    /// <inheritdoc/>
    public string Name => "datetime";

    /// <inheritdoc/>
    public string Description => "Gets the current date and time in various formats. Can also convert between timezones and formats.";

    /// <inheritdoc/>
    public JsonElement InputSchema => _inputSchema;

    /// <inheritdoc/>
    public Task<ToolResult> ExecuteAsync(
        object input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var parameters = ParseInput(input);

        var format = "iso";
        if (parameters.TryGetValue("format", out var formatObj) && formatObj is string formatStr)
        {
            format = formatStr.ToLowerInvariant();
        }

        var timezone = "UTC";
        if (parameters.TryGetValue("timezone", out var timezoneObj) && timezoneObj is string timezoneStr)
        {
            timezone = timezoneStr;
        }

        try
        {
            var now = DateTimeOffset.UtcNow;

            // Convert timezone if specified
            if (timezone != "UTC")
            {
                try
                {
                    var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                    now = TimeZoneInfo.ConvertTime(now, timeZoneInfo);
                }
                catch (TimeZoneNotFoundException)
                {
                    // Try alternative timezone formats
                    try
                    {
                        // Try Windows timezone format
                        var windowsTimezone = ConvertToWindowsTimezone(timezone);
                        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(windowsTimezone);
                        now = TimeZoneInfo.ConvertTime(now, timeZoneInfo);
                    }
                    catch
                    {
                        return Task.FromResult(ToolResult.Failure(
                            $"Invalid timezone: {timezone}",
                            new Dictionary<string, object>
                            {
                                ["timezone"] = timezone
                            }));
                    }
                }
            }

            string output;
            switch (format)
            {
                case "iso":
                    output = now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                    break;
                case "unix":
                    output = now.ToUnixTimeSeconds().ToString();
                    break;
                case "readable":
                    output = now.ToString("yyyy-MM-dd HH:mm:ss zzz");
                    break;
                default:
                    // Custom format
                    try
                    {
                        output = now.ToString(format);
                    }
                    catch (FormatException ex)
                    {
                        return Task.FromResult(ToolResult.Failure(
                            $"Invalid format string: {ex.Message}",
                            new Dictionary<string, object>
                            {
                                ["format"] = format
                            }));
                    }
                    break;
            }

            return Task.FromResult(ToolResult.Success(
                output,
                new Dictionary<string, object>
                {
                    ["datetime"] = now.ToString("O"), // ISO 8601
                    ["unix_timestamp"] = now.ToUnixTimeSeconds(),
                    ["timezone"] = timezone,
                    ["format"] = format
                }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Failure(
                $"Failed to get datetime: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["format"] = format,
                    ["timezone"] = timezone
                }));
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

    private static string ConvertToWindowsTimezone(string ianaTimezone)
    {
        // Common IANA to Windows timezone mappings
        return ianaTimezone switch
        {
            "America/New_York" => "Eastern Standard Time",
            "America/Chicago" => "Central Standard Time",
            "America/Denver" => "Mountain Standard Time",
            "America/Los_Angeles" => "Pacific Standard Time",
            "Europe/London" => "GMT Standard Time",
            "Europe/Paris" => "W. Europe Standard Time",
            "Asia/Tokyo" => "Tokyo Standard Time",
            "Asia/Shanghai" => "China Standard Time",
            _ => ianaTimezone // Return as-is if no mapping found
        };
    }
}
