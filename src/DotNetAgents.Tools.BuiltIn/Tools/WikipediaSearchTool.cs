// SPDX-License-Identifier: Apache-2.0

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetAgents.Abstractions.Tools;

using DotNetAgents.Abstractions.Exceptions;

namespace DotNetAgents.Tools.BuiltIn;

/// <summary>
/// A tool for searching Wikipedia and retrieving article summaries.
/// </summary>
public class WikipediaSearchTool : ITool
{
    private readonly HttpClient _httpClient;
    private static readonly JsonElement _inputSchema;

    static WikipediaSearchTool()
    {
        _inputSchema = JsonSerializer.Deserialize<JsonElement>(@"{
            ""type"": ""object"",
            ""properties"": {
                ""query"": {
                    ""type"": ""string"",
                    ""description"": ""The search query or article title""
                },
                ""max_results"": {
                    ""type"": ""integer"",
                    ""description"": ""Maximum number of results to return. Default: 5""
                },
                ""extract_length"": {
                    ""type"": ""integer"",
                    ""description"": ""Length of extract in characters. Default: 500""
                }
            },
            ""required"": [""query""]
        }");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WikipediaSearchTool"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for Wikipedia API requests.</param>
    public WikipediaSearchTool(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc/>
    public string Name => "wikipedia_search";

    /// <inheritdoc/>
    public string Description => "Searches Wikipedia and retrieves article summaries. Uses the Wikipedia API to fetch article content and extracts.";

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
            else if (maxResultsObj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Number)
            {
                maxResults = Math.Clamp(jsonElement.GetInt32(), 1, 10);
            }
        }

        var extractLength = 500;
        if (parameters.TryGetValue("extract_length", out var extractLengthObj))
        {
            if (extractLengthObj is int extractLengthInt)
            {
                extractLength = Math.Clamp(extractLengthInt, 100, 2000);
            }
            else if (extractLengthObj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Number)
            {
                extractLength = Math.Clamp(jsonElement.GetInt32(), 100, 2000);
            }
        }

        try
        {
            // Use Wikipedia API
            var encodedQuery = Uri.EscapeDataString(query);
            var url = $"https://en.wikipedia.org/api/rest_v1/page/summary/{encodedQuery}";

            var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Try search API instead
                return await SearchWikipediaAsync(query, maxResults, extractLength, cancellationToken).ConfigureAwait(false);
            }

            response.EnsureSuccessStatusCode();

            var summary = await response.Content.ReadFromJsonAsync<WikipediaSummary>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken).ConfigureAwait(false);

            if (summary == null)
            {
                return ToolResult.Failure("Failed to parse Wikipedia response.");
            }

            var result = new Dictionary<string, object>
            {
                ["title"] = summary.Title ?? query,
                ["extract"] = TruncateText(summary.Extract ?? "", extractLength),
                ["url"] = summary.ContentUrls?.Desktop?.Page ?? "",
                ["thumbnail"] = summary.Thumbnail?.Source ?? ""
            };

            return ToolResult.Success(
                JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }),
                new Dictionary<string, object>
                {
                    ["query"] = query,
                    ["title"] = summary.Title ?? query
                });
        }
        catch (HttpRequestException ex)
        {
            return ToolResult.Failure(
                $"Wikipedia API request failed: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["query"] = query
                });
        }
        catch (Exception ex)
        {
            return ToolResult.Failure(
                $"Wikipedia search failed: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["query"] = query
                });
        }
    }

    private async Task<ToolResult> SearchWikipediaAsync(
        string query,
        int maxResults,
        int extractLength,
        CancellationToken cancellationToken)
    {
        try
        {
            var encodedQuery = Uri.EscapeDataString(query);
            var url = $"https://en.wikipedia.org/api/rest_v1/page/search/{encodedQuery}?limit={maxResults}";

            var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var searchResults = await response.Content.ReadFromJsonAsync<WikipediaSearchResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken).ConfigureAwait(false);

            if (searchResults?.Pages == null || searchResults.Pages.Count == 0)
            {
                return ToolResult.Success(
                    $"No Wikipedia articles found for: {query}",
                    new Dictionary<string, object>
                    {
                        ["query"] = query,
                        ["results_count"] = 0
                    });
            }

            var results = searchResults.Pages.Take(maxResults).Select(page => new Dictionary<string, object>
            {
                ["title"] = page.Title ?? "",
                ["extract"] = TruncateText(page.Extract ?? "", extractLength),
                ["url"] = $"https://en.wikipedia.org/wiki/{Uri.EscapeDataString(page.Title ?? "")}"
            }).ToList();

            return ToolResult.Success(
                JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }),
                new Dictionary<string, object>
                {
                    ["query"] = query,
                    ["results_count"] = results.Count
                });
        }
        catch (Exception ex)
        {
            return ToolResult.Failure($"Wikipedia search failed: {ex.Message}");
        }
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        // Try to truncate at word boundary
        var truncated = text.Substring(0, maxLength);
        var lastSpace = truncated.LastIndexOf(' ');
        if (lastSpace > maxLength * 0.8) // Only truncate at word if we're not losing too much
        {
            truncated = truncated.Substring(0, lastSpace);
        }

        return truncated + "...";
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

    private record WikipediaSummary
    {
        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("extract")]
        public string? Extract { get; init; }

        [JsonPropertyName("content_urls")]
        public WikipediaUrls? ContentUrls { get; init; }

        [JsonPropertyName("thumbnail")]
        public WikipediaThumbnail? Thumbnail { get; init; }
    }

    private record WikipediaUrls
    {
        [JsonPropertyName("desktop")]
        public WikipediaPage? Desktop { get; init; }
    }

    private record WikipediaPage
    {
        [JsonPropertyName("page")]
        public string? Page { get; init; }
    }

    private record WikipediaThumbnail
    {
        [JsonPropertyName("source")]
        public string? Source { get; init; }
    }

    private record WikipediaSearchResponse
    {
        [JsonPropertyName("pages")]
        public List<WikipediaPageResult> Pages { get; init; } = new();
    }

    private record WikipediaPageResult
    {
        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("extract")]
        public string? Extract { get; init; }
    }
}
