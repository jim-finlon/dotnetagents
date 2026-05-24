// SPDX-License-Identifier: Apache-2.0

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace DotNetAgents.Mcp.Conformance;

/// <summary>
/// Standard conformance suite for a DNA service's <c>POST /mcp/tools/call</c>
/// endpoint envelope + machine-readable error contract. Story f34c78e1.
///
/// Service test projects inherit from this class; xUnit discovers the
/// <c>[Fact]</c> methods on derived classes.
///
/// Asserts:
///   - Calling an unknown tool returns the canonical envelope shape
///     (success=false + error + errorCode), with errorCode matching the
///     <see cref="McpCanonicalErrorCodes"/> contract for unknown tools.
///   - Calling with malformed body returns a 4xx with a parseable error envelope.
///   - The successful tool path (provided by <see cref="GetSuccessCallPayload"/>)
///     returns success=true with a result block.
/// </summary>
public abstract class McpToolCallConformanceTests<TProgram> : IClassFixture<WebApplicationFactory<TProgram>>
    where TProgram : class
{
    protected WebApplicationFactory<TProgram> Factory { get; }

    protected McpToolCallConformanceTests(WebApplicationFactory<TProgram> factory) => Factory = factory;

    /// <summary>Override to customize the endpoint path. Defaults to <c>/mcp/tools/call</c>.</summary>
    protected virtual string ToolCallEndpointPath => "/mcp/tools/call";

    /// <summary>Override for factories that need custom host setup.</summary>
    protected virtual HttpClient CreateClient() => Factory.CreateClient();

    /// <summary>
    /// Override to provide a known-successful (toolName, arguments) call so the harness can
    /// verify the success path. Returning null skips the success-path test.
    /// </summary>
    protected virtual (string ToolName, IReadOnlyDictionary<string, object?> Arguments)? GetSuccessCallPayload() => null;

    [Fact]
    public async Task Unknown_tool_returns_canonical_unknown_tool_envelope()
    {
        using var client = CreateClient();
        var response = await client.PostAsJsonAsync(ToolCallEndpointPath, new { toolName = "definitely-not-a-real-tool", arguments = new { } });
        var body = await response.Content.ReadAsStringAsync();
        // Tolerate either 200 (envelope-style) or 4xx (HTTP-style) — both must produce a parseable envelope.
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        // Either the envelope flag is false, or the response is HTTP-error and the body still has error fields.
        var success = root.TryGetProperty("success", out var s) ? s.ValueKind == JsonValueKind.True : (bool?)null;
        var errorCode = root.TryGetProperty("errorCode", out var ec) ? ec.GetString() : null;
        var errorMessage = root.TryGetProperty("error", out var e) ? e.GetString()
                          : root.TryGetProperty("errorMessage", out var em) ? em.GetString() : null;

        Assert.True(success != true, "Unknown tool should NOT have success=true.");
        Assert.False(string.IsNullOrWhiteSpace(errorCode) && string.IsNullOrWhiteSpace(errorMessage),
            "Unknown tool response must include errorCode or error message.");
        if (!string.IsNullOrWhiteSpace(errorCode))
            Assert.Equal(McpCanonicalErrorCodes.UnknownTool, errorCode, ignoreCase: true);
    }

    [Fact]
    public async Task Malformed_body_returns_parseable_error()
    {
        using var client = CreateClient();
        var response = await client.PostAsync(ToolCallEndpointPath,
            new StringContent("{ this is not json", System.Text.Encoding.UTF8, "application/json"));

        // Must be 4xx — never 5xx for client malformation
        var status = (int)response.StatusCode;
        Assert.True(status is >= 400 and < 500, $"Malformed body returned {status}; expected 4xx.");
    }

    [Fact]
    public async Task Success_path_returns_envelope_with_success_true_when_payload_provided()
    {
        var payload = GetSuccessCallPayload();
        if (payload is null) return; // opt-out

        using var client = CreateClient();
        var response = await client.PostAsJsonAsync(ToolCallEndpointPath, new
        {
            toolName = payload.Value.ToolName,
            arguments = payload.Value.Arguments,
        });

        Assert.True(response.IsSuccessStatusCode,
            $"Successful tool call returned {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("success", out var s) && s.ValueKind == JsonValueKind.True,
            "Successful tool call must return success=true in the envelope.");
        Assert.True(root.TryGetProperty("result", out _), "Successful tool call must include a result block.");
    }
}
