// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;
using HtmlAgilityPack;
using DotNetAgents.Abstractions.Tools;

using DotNetAgents.Abstractions.Exceptions;

namespace DotNetAgents.Tools.BuiltIn;

/// <summary>
/// A tool for fetching and parsing content from URLs.
/// </summary>
public class UrlFetchTool : ITool
{
    private readonly HttpClient _httpClient;
    private static readonly System.Text.Json.JsonElement _inputSchema;

    static UrlFetchTool()
    {
        _inputSchema = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(@"{
            ""type"": ""object"",
            ""properties"": {
                ""url"": {
                    ""type"": ""string"",
                    ""description"": ""The URL to fetch content from""
                },
                ""extract_text"": {
                    ""type"": ""boolean"",
                    ""description"": ""Whether to extract plain text from HTML. Default: true""
                },
                ""extract_links"": {
                    ""type"": ""boolean"",
                    ""description"": ""Whether to extract links from HTML. Default: false""
                },
                ""extract_images"": {
                    ""type"": ""boolean"",
                    ""description"": ""Whether to extract image URLs from HTML. Default: false""
                },
                ""timeout"": {
                    ""type"": ""integer"",
                    ""description"": ""Request timeout in seconds. Default: 30""
                }
            },
            ""required"": [""url""]
        }");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UrlFetchTool"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for requests.</param>
    public UrlFetchTool(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        // Set default user agent
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "DotNetAgents/1.0");
        }
    }

    /// <inheritdoc/>
    public string Name => "url_fetch";

    /// <inheritdoc/>
    public string Description => "Fetches content from URLs and extracts text, links, and images from HTML pages.";

    /// <inheritdoc/>
    public System.Text.Json.JsonElement InputSchema => _inputSchema;

    /// <inheritdoc/>
    public async Task<ToolResult> ExecuteAsync(
        object input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var parameters = ParseInput(input);
        if (!parameters.TryGetValue("url", out var urlObj) || urlObj is not string url)
        {
            return ToolResult.Failure("Missing or invalid 'url' parameter.");
        }

        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return ToolResult.Failure($"Invalid URL: {url}");
        }

        var extractText = true;
        if (parameters.TryGetValue("extract_text", out var extractTextObj))
        {
            extractText = extractTextObj is bool b ? b : extractTextObj is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.True;
        }

        var extractLinks = false;
        if (parameters.TryGetValue("extract_links", out var extractLinksObj))
        {
            extractLinks = extractLinksObj is bool b ? b : extractLinksObj is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.True;
        }

        var extractImages = false;
        if (parameters.TryGetValue("extract_images", out var extractImagesObj))
        {
            extractImages = extractImagesObj is bool b ? b : extractImagesObj is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.True;
        }

        var timeout = TimeSpan.FromSeconds(30);
        if (parameters.TryGetValue("timeout", out var timeoutObj))
        {
            if (timeoutObj is int timeoutInt && timeoutInt > 0)
            {
                timeout = TimeSpan.FromSeconds(timeoutInt);
            }
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            var response = await _httpClient.GetAsync(uri, cts.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "text/html";
            var content = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);

            var result = new Dictionary<string, object>
            {
                ["url"] = url,
                ["content_type"] = contentType,
                ["status_code"] = (int)response.StatusCode,
                ["content_length"] = content.Length
            };

            // Extract content based on type
            if (contentType.Contains("html", StringComparison.OrdinalIgnoreCase))
            {
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(content);

                if (extractText)
                {
                    var text = htmlDoc.DocumentNode.InnerText;
                    result["text"] = Regex.Replace(text, @"\s+", " ").Trim();
                }

                if (extractLinks)
                {
                    var links = htmlDoc.DocumentNode.SelectNodes("//a[@href]")
                        ?.Select(a => new Dictionary<string, object>
                        {
                            ["href"] = a.GetAttributeValue("href", ""),
                            ["text"] = a.InnerText.Trim()
                        })
                        .ToList() ?? new List<Dictionary<string, object>>();
                    result["links"] = links;
                }

                if (extractImages)
                {
                    var images = htmlDoc.DocumentNode.SelectNodes("//img[@src]")
                        ?.Select(img => new Dictionary<string, object>
                        {
                            ["src"] = img.GetAttributeValue("src", ""),
                            ["alt"] = img.GetAttributeValue("alt", "")
                        })
                        .ToList() ?? new List<Dictionary<string, object>>();
                    result["images"] = images;
                }

                result["title"] = htmlDoc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim() ?? "";
            }
            else
            {
                // For non-HTML content, return raw content
                result["content"] = content;
            }

            return ToolResult.Success(
                System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
                new Dictionary<string, object>
                {
                    ["url"] = url,
                    ["content_type"] = contentType,
                    ["content_length"] = content.Length
                });
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            return ToolResult.Failure(
                $"Request timed out after {timeout.TotalSeconds} seconds.",
                new Dictionary<string, object>
                {
                    ["url"] = url
                });
        }
        catch (HttpRequestException ex)
        {
            return ToolResult.Failure(
                $"HTTP request failed: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["url"] = url
                });
        }
        catch (Exception ex)
        {
            return ToolResult.Failure(
                $"Failed to fetch URL: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["url"] = url
                });
        }
    }

    private static IDictionary<string, object> ParseInput(object input)
    {
        if (input is System.Text.Json.JsonElement jsonElement)
        {
            var dict = new Dictionary<string, object>();
            foreach (var prop in jsonElement.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.String => prop.Value.GetString()!,
                    System.Text.Json.JsonValueKind.Number => prop.Value.GetDouble(),
                    System.Text.Json.JsonValueKind.True => true,
                    System.Text.Json.JsonValueKind.False => false,
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
