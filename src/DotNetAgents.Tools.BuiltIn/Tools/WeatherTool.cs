using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetAgents.Abstractions.Tools;

using DotNetAgents.Abstractions.Exceptions;

namespace DotNetAgents.Tools.BuiltIn;

/// <summary>
/// A tool for retrieving weather information.
/// Note: This is a placeholder implementation. For production use, integrate with a weather API like OpenWeatherMap.
/// </summary>
public class WeatherTool : ITool
{
    private readonly HttpClient? _httpClient;
    private readonly string? _apiKey;
    private static readonly JsonElement _inputSchema;

    static WeatherTool()
    {
        _inputSchema = JsonSerializer.Deserialize<JsonElement>(@"{
            ""type"": ""object"",
            ""properties"": {
                ""location"": {
                    ""type"": ""string"",
                    ""description"": ""City name or location (e.g., 'London', 'New York', 'Paris')""
                },
                ""units"": {
                    ""type"": ""string"",
                    ""description"": ""Temperature units: 'celsius' or 'fahrenheit'. Default: 'celsius'"",
                    ""enum"": [""celsius"", ""fahrenheit""]
                }
            },
            ""required"": [""location""]
        }");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherTool"/> class.
    /// </summary>
    /// <param name="httpClient">Optional HTTP client for API requests.</param>
    /// <param name="apiKey">Optional API key for weather service (e.g., OpenWeatherMap).</param>
    public WeatherTool(HttpClient? httpClient = null, string? apiKey = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _apiKey = apiKey;
    }

    /// <inheritdoc/>
    public string Name => "weather";

    /// <inheritdoc/>
    public string Description => "Retrieves weather information for a given location. Requires a weather API key (e.g., OpenWeatherMap) for full functionality.";

    /// <inheritdoc/>
    public JsonElement InputSchema => _inputSchema;

    /// <inheritdoc/>
    public async Task<ToolResult> ExecuteAsync(
        object input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var parameters = ParseInput(input);
        if (!parameters.TryGetValue("location", out var locationObj) || locationObj is not string location)
        {
            return ToolResult.Failure("Missing or invalid 'location' parameter.");
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            return ToolResult.Failure("Location cannot be null or empty.");
        }

        var units = "celsius";
        if (parameters.TryGetValue("units", out var unitsObj) && unitsObj is string unitsStr)
        {
            units = unitsStr.ToLowerInvariant();
        }

        // If no API key, return a placeholder response
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return ToolResult.Success(
                $"Weather information for {location} is not available. To enable weather functionality, provide an API key from a weather service (e.g., OpenWeatherMap) when initializing the WeatherTool.",
                new Dictionary<string, object>
                {
                    ["location"] = location,
                    ["note"] = "Weather API key required for full functionality"
                });
        }

        try
        {
            // Example: OpenWeatherMap API integration
            // This is a placeholder - implement with actual API
            var encodedLocation = Uri.EscapeDataString(location);
            var unitParam = units == "fahrenheit" ? "imperial" : "metric";
            var url = $"https://api.openweathermap.org/data/2.5/weather?q={encodedLocation}&appid={_apiKey}&units={unitParam}";

            var response = await _httpClient!.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var weatherData = await response.Content.ReadFromJsonAsync<OpenWeatherMapResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken).ConfigureAwait(false);

            if (weatherData == null)
            {
                return ToolResult.Failure("Failed to parse weather response.");
            }

            var result = new Dictionary<string, object>
            {
                ["location"] = weatherData.Name ?? location,
                ["temperature"] = weatherData.Main?.Temp ?? 0,
                ["feels_like"] = weatherData.Main?.FeelsLike ?? 0,
                ["humidity"] = weatherData.Main?.Humidity ?? 0,
                ["pressure"] = weatherData.Main?.Pressure ?? 0,
                ["description"] = weatherData.Weather?.FirstOrDefault()?.Description ?? "",
                ["wind_speed"] = weatherData.Wind?.Speed ?? 0,
                ["units"] = units
            };

            return ToolResult.Success(
                JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }),
                new Dictionary<string, object>
                {
                    ["location"] = weatherData.Name ?? location,
                    ["temperature"] = weatherData.Main?.Temp ?? 0
                });
        }
        catch (HttpRequestException ex)
        {
            return ToolResult.Failure(
                $"Weather API request failed: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["location"] = location
                });
        }
        catch (Exception ex)
        {
            return ToolResult.Failure(
                $"Weather lookup failed: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["location"] = location
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

    private record OpenWeatherMapResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("main")]
        public WeatherMain? Main { get; init; }

        [JsonPropertyName("weather")]
        public List<WeatherDescription>? Weather { get; init; }

        [JsonPropertyName("wind")]
        public WeatherWind? Wind { get; init; }
    }

    private record WeatherMain
    {
        [JsonPropertyName("temp")]
        public double Temp { get; init; }

        [JsonPropertyName("feels_like")]
        public double FeelsLike { get; init; }

        [JsonPropertyName("humidity")]
        public int Humidity { get; init; }

        [JsonPropertyName("pressure")]
        public int Pressure { get; init; }
    }

    private record WeatherDescription
    {
        [JsonPropertyName("description")]
        public string? Description { get; init; }
    }

    private record WeatherWind
    {
        [JsonPropertyName("speed")]
        public double Speed { get; init; }
    }
}
