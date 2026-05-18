using System.Net.Http.Json;
using System.Text.Json;
using DotNetAgents.Abstractions.Exceptions;
using DotNetAgents.Abstractions.Tools;


namespace DotNetAgents.Tools.BuiltIn;

/// <summary>
/// A tool for making HTTP requests to REST APIs.
/// </summary>
public class HttpClientTool : ITool
{
    private readonly HttpClient _httpClient;
    private static readonly JsonElement _inputSchema;

    static HttpClientTool()
    {
        _inputSchema = JsonSerializer.Deserialize<JsonElement>(@"{
            ""type"": ""object"",
            ""properties"": {
                ""url"": {
                    ""type"": ""string"",
                    ""description"": ""The URL to make the request to""
                },
                ""method"": {
                    ""type"": ""string"",
                    ""description"": ""HTTP method (GET, POST, PUT, DELETE, PATCH). Default: GET"",
                    ""enum"": [""GET"", ""POST"", ""PUT"", ""DELETE"", ""PATCH""]
                },
                ""headers"": {
                    ""type"": ""object"",
                    ""description"": ""Optional HTTP headers as key-value pairs""
                },
                ""body"": {
                    ""type"": ""string"",
                    ""description"": ""Request body (for POST, PUT, PATCH)""
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
    /// Initializes a new instance of the <see cref="HttpClientTool"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for requests.</param>
    public HttpClientTool(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc/>
    public string Name => "http_client";

    /// <inheritdoc/>
    public string Description => "Makes HTTP requests to REST APIs. Supports GET, POST, PUT, DELETE, and PATCH methods with custom headers and request bodies.";

    /// <inheritdoc/>
    public JsonElement InputSchema => _inputSchema;

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

        var method = HttpMethod.Get;
        if (parameters.TryGetValue("method", out var methodObj) && methodObj is string methodStr)
        {
            method = methodStr.ToUpperInvariant() switch
            {
                "GET" => HttpMethod.Get,
                "POST" => HttpMethod.Post,
                "PUT" => HttpMethod.Put,
                "DELETE" => HttpMethod.Delete,
                "PATCH" => new HttpMethod("PATCH"),
                _ => HttpMethod.Get
            };
        }

        var timeout = TimeSpan.FromSeconds(30);
        if (parameters.TryGetValue("timeout", out var timeoutObj))
        {
            if (timeoutObj is int timeoutInt && timeoutInt > 0)
            {
                timeout = TimeSpan.FromSeconds(timeoutInt);
            }
            else if (timeoutObj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Number)
            {
                var timeoutValue = jsonElement.GetInt32();
                if (timeoutValue > 0)
                {
                    timeout = TimeSpan.FromSeconds(timeoutValue);
                }
            }
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            using var request = new HttpRequestMessage(method, uri);

            // Add headers
            if (parameters.TryGetValue("headers", out var headersObj))
            {
                AddHeaders(request, headersObj);
            }

            // Add body for POST, PUT, PATCH
            if ((method == HttpMethod.Post || method == HttpMethod.Put || method.Method == "PATCH") &&
                parameters.TryGetValue("body", out var bodyObj) && bodyObj is string body)
            {
                request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);

            var result = new Dictionary<string, object>
            {
                ["status_code"] = (int)response.StatusCode,
                ["status_text"] = response.StatusCode.ToString(),
                ["headers"] = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                ["body"] = responseBody
            };

            if (response.Content.Headers.ContentType != null)
            {
                result["content_type"] = response.Content.Headers.ContentType.ToString();
            }

            return ToolResult.Success(
                JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }),
                new Dictionary<string, object>
                {
                    ["url"] = url,
                    ["method"] = method.Method,
                    ["status_code"] = (int)response.StatusCode,
                    ["success"] = response.IsSuccessStatusCode
                });
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            return ToolResult.Failure(
                $"Request timed out after {timeout.TotalSeconds} seconds.",
                new Dictionary<string, object>
                {
                    ["url"] = url,
                    ["method"] = method.Method
                });
        }
        catch (HttpRequestException ex)
        {
            return ToolResult.Failure(
                $"HTTP request failed: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["url"] = url,
                    ["method"] = method.Method
                });
        }
        catch (Exception ex)
        {
            return ToolResult.Failure(
                $"Request failed: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["url"] = url,
                    ["method"] = method.Method
                });
        }
    }

    private static void AddHeaders(HttpRequestMessage request, object headersObj)
    {
        if (headersObj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in jsonElement.EnumerateObject())
            {
                var value = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString()!,
                    JsonValueKind.Number => prop.Value.GetDouble().ToString(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => prop.Value.ToString()
                };
                request.Headers.TryAddWithoutValidation(prop.Name, value);
            }
        }
        else if (headersObj is IDictionary<string, object> headersDict)
        {
            foreach (var kvp in headersDict)
            {
                request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value?.ToString() ?? string.Empty);
            }
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
                    JsonValueKind.Object => prop.Value,
                    JsonValueKind.Array => prop.Value,
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
