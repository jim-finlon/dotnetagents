using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DotNetAgents.Abstractions.Exceptions;
using DotNetAgents.Abstractions.Tools;

namespace DotNetAgents.Tools.BuiltIn;

/// <summary>
/// A tool for sending messages to Chat channels via the Chat Web API.
/// </summary>
public class ChatTool : ITool
{
    private readonly HttpClient _httpClient;
    private readonly string _botToken;
    private static readonly JsonElement _inputSchema;

    static ChatTool()
    {
        _inputSchema = JsonSerializer.Deserialize<JsonElement>(@"{
            ""type"": ""object"",
            ""properties"": {
                ""channel"": {
                    ""type"": ""string"",
                    ""description"": ""Chat channel ID or name (e.g., #general or C1234567890)""
                },
                ""text"": {
                    ""type"": ""string"",
                    ""description"": ""Message text to send""
                },
                ""thread_ts"": {
                    ""type"": ""string"",
                    ""description"": ""Optional thread timestamp to reply in a thread""
                },
                ""blocks"": {
                    ""type"": ""array"",
                    ""description"": ""Optional Chat Block Kit blocks for rich formatting""
                }
            },
            ""required"": [""channel"", ""text""]
        }");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatTool"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for making requests.</param>
    /// <param name="botToken">The Chat bot token (starts with 'xoxb-').</param>
    /// <exception cref="ArgumentNullException">Thrown when httpClient or botToken is null.</exception>
    public ChatTool(HttpClient httpClient, string botToken)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _botToken = botToken ?? throw new ArgumentNullException(nameof(botToken));

        _httpClient.BaseAddress = new Uri("https://chat.com/api/");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_botToken}");
    }

    /// <inheritdoc/>
    public string Name => "chat_send_message";

    /// <inheritdoc/>
    public string Description => "Sends a message to a Chat channel using the Chat Web API.";

    /// <inheritdoc/>
    public JsonElement InputSchema => _inputSchema;

    /// <inheritdoc/>
    public async Task<ToolResult> ExecuteAsync(
        object input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input is not IDictionary<string, object> inputDict)
        {
            return ToolResult.Failure("Input must be a dictionary.");
        }

        try
        {
            if (!inputDict.TryGetValue("channel", out var channelObj) || channelObj == null)
            {
                return ToolResult.Failure("Channel is required.");
            }

            if (!inputDict.TryGetValue("text", out var textObj) || textObj == null)
            {
                return ToolResult.Failure("Text is required.");
            }

            var channel = channelObj.ToString() ?? string.Empty;
            var text = textObj.ToString() ?? string.Empty;

            // Build request payload
            var payload = new Dictionary<string, object>
            {
                ["channel"] = channel,
                ["text"] = text
            };

            if (inputDict.TryGetValue("thread_ts", out var threadTsObj) && threadTsObj != null)
            {
                payload["thread_ts"] = threadTsObj.ToString() ?? string.Empty;
            }

            if (inputDict.TryGetValue("blocks", out var blocksObj) && blocksObj != null)
            {
                payload["blocks"] = blocksObj;
            }

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "chat.postMessage",
                content,
                cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return ToolResult.Failure($"Chat API error: {response.StatusCode}. {errorContent}");
            }

            var result = await response.Content.ReadFromJsonAsync<ChatResponse>(
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (result?.Ok == true)
            {
                return ToolResult.Success(
                    $"Message sent successfully to channel {channel}. Timestamp: {result.Ts}",
                    new Dictionary<string, object>
                    {
                        ["channel"] = channel,
                        ["ts"] = result.Ts ?? string.Empty,
                        ["message"] = result.Message ?? new Dictionary<string, object>()
                    });
            }

            return ToolResult.Failure($"Chat API error: {result?.Error ?? "Unknown error"}");
        }
        catch (Exception ex)
        {
            return ToolResult.Failure($"Failed to send Chat message: {ex.Message}");
        }
    }

    private class ChatResponse
    {
        public bool Ok { get; set; }
        public string? Error { get; set; }
        public string? Ts { get; set; }
        public Dictionary<string, object>? Message { get; set; }
    }
}
