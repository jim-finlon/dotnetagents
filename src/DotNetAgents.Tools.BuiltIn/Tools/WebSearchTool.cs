using System.Net.Http.Json;
using System.Text.Json;
using DotNetAgents.Abstractions.Exceptions;
using DotNetAgents.Abstractions.Tools;


namespace DotNetAgents.Tools.BuiltIn;

/// <summary>
/// A web search tool that uses DuckDuckGo Instant Answer API for search results.
/// Note: This is a placeholder implementation. For production use, integrate with a proper search API.
/// </summary>
public class WebSearchTool : ITool
{
    private readonly HttpClient? _httpClient;
    private static readonly JsonElement _inputSchema;

    static WebSearchTool()
    {
        _inputSchema = JsonSerializer.Deserialize<JsonElement>(@"{
            ""type"": ""object"",
            ""properties"": {
                ""query"": {
                    ""type"": ""string"",
                    ""description"": ""The search query to execute""
                },
                ""max_results"": {
                    ""type"": ""integer"",
                    ""description"": ""Maximum number of results to return (default: 5, max: 10)""
                }
            },
            ""required"": [""query""]
        }");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebSearchTool"/> class.
    /// </summary>
    /// <param name="httpClient">Optional HTTP client for making search requests. If null, a new instance will be created.</param>
    public WebSearchTool(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    /// <inheritdoc/>
    public string Name => "web_search";

    /// <inheritdoc/>
    public string Description => "Searches the web for information. Returns search results and summaries.";

    /// <inheritdoc/>
    public JsonElement InputSchema => _inputSchema;

    /// <inheritdoc/>
    public async Task<ToolResult> ExecuteAsync(
        object input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var parameters = ParseInput(input);
        if (!parameters.TryGetValue("query", out var queryObj) || queryObj is not string query)
        {
            return ToolResult.Failure("Missing or invalid 'query' parameter.");
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return ToolResult.Failure("Query cannot be null or empty.");
        }

        var maxResults = 5;
        if (parameters.TryGetValue("max_results", out var maxResultsObj))
        {
            if (maxResultsObj is int maxResultsInt)
            {
                maxResults = Math.Clamp(maxResultsInt, 1, 10);
            }
            else if (maxResultsObj is long maxResultsLong)
            {
                maxResults = Math.Clamp((int)maxResultsLong, 1, 10);
            }
            else if (maxResultsObj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Number)
            {
                maxResults = Math.Clamp(jsonElement.GetInt32(), 1, 10);
            }
        }

        try
        {
            // Use DuckDuckGo Instant Answer API (no API key required)
            var encodedQuery = Uri.EscapeDataString(query);
            var url = $"https://api.duckduckgo.com/?q={encodedQuery}&format=json&no_html=1&skip_disambig=1";

            var response = await _httpClient!.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<DuckDuckGoResponse>(json);

            var results = new List<Dictionary<string, object>>();

            // Extract abstract/summary
            if (!string.IsNullOrWhiteSpace(result?.AbstractText))
            {
                results.Add(new Dictionary<string, object>
                {
                    ["title"] = result.Abstract ?? "Summary",
                    ["snippet"] = result.AbstractText,
                    ["url"] = result.AbstractURL ?? "",
                    ["type"] = "abstract"
                });
            }

            // Extract related topics
            if (result?.RelatedTopics != null)
            {
                foreach (var topic in result.RelatedTopics.Take(maxResults - results.Count))
                {
                    if (!string.IsNullOrWhiteSpace(topic.Text))
                    {
                        results.Add(new Dictionary<string, object>
                        {
                            ["title"] = topic.Text.Split(" - ").FirstOrDefault() ?? "Result",
                            ["snippet"] = topic.Text,
                            ["url"] = topic.FirstURL ?? "",
                            ["type"] = "related_topic"
                        });
                    }
                }
            }

            // If no results, return a message
            if (results.Count == 0)
            {
                return ToolResult.Success(
                    $"No results found for query: {query}",
                    new Dictionary<string, object>
                    {
                        ["query"] = query,
                        ["results_count"] = 0
                    });
            }

            return ToolResult.Success(
                JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }),
                new Dictionary<string, object>
                {
                    ["query"] = query,
                    ["results_count"] = results.Count
                });
        }
        catch (HttpRequestException ex)
        {
            return ToolResult.Failure(
                $"Web search request failed: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["query"] = query
                });
        }
        catch (Exception ex)
        {
            return ToolResult.Failure(
                $"Web search failed: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["query"] = query
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

    private record DuckDuckGoResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("AbstractText")]
        public string? AbstractText { get; init; }

        [System.Text.Json.Serialization.JsonPropertyName("Abstract")]
        public string? Abstract { get; init; }

        [System.Text.Json.Serialization.JsonPropertyName("AbstractURL")]
        public string? AbstractURL { get; init; }

        [System.Text.Json.Serialization.JsonPropertyName("RelatedTopics")]
        public List<RelatedTopic>? RelatedTopics { get; init; }
    }

    private record RelatedTopic
    {
        [System.Text.Json.Serialization.JsonPropertyName("Text")]
        public string Text { get; init; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("FirstURL")]
        public string? FirstURL { get; init; }
    }
}
