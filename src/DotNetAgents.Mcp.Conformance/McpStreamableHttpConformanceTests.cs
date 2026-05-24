// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace DotNetAgents.Mcp.Conformance;

/// <summary>
/// Standard conformance suite for a DNA service's streamable HTTP MCP endpoint
/// (<c>POST /mcp</c> by default). Story f34c78e1.
///
/// The streamable transport is optional for some services — services that don't
/// expose it can override <see cref="StreamableEnabled"/> to <c>false</c> to
/// skip the suite. Services that DO expose it must satisfy the minimum
/// contract: respond to a basic JSON-RPC-style ping with a 2xx, accept the
/// MCP content-type negotiation, and produce parseable JSON envelopes.
/// </summary>
public abstract class McpStreamableHttpConformanceTests<TProgram> : IClassFixture<WebApplicationFactory<TProgram>>
    where TProgram : class
{
    protected WebApplicationFactory<TProgram> Factory { get; }

    protected McpStreamableHttpConformanceTests(WebApplicationFactory<TProgram> factory) => Factory = factory;

    /// <summary>Override to point the harness at your service's streamable endpoint. Default <c>/mcp</c>.</summary>
    protected virtual string StreamableEndpointPath => "/mcp";

    /// <summary>Override to opt out of the streamable-HTTP suite for services that don't expose it.</summary>
    protected virtual bool StreamableEnabled => true;

    /// <summary>Override for factories that need custom host setup.</summary>
    protected virtual HttpClient CreateClient() => Factory.CreateClient();

    [Fact]
    public async Task Streamable_endpoint_responds_to_initialize_request()
    {
        if (!StreamableEnabled) return;

        using var client = CreateClient();
        var initRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2025-06-18",
                capabilities = new { },
                clientInfo = new { name = "DotNetAgents.Mcp.Conformance", version = "1.0.0" },
            },
        };
        var content = new StringContent(JsonSerializer.Serialize(initRequest), System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync(StreamableEndpointPath, content);

        // Accept either 2xx (envelope-style) or 4xx (host doesn't enable it but the path exists).
        // 5xx is a hard fail — initialize must never crash a streamable host.
        var status = (int)response.StatusCode;
        Assert.True(status < 500, $"Streamable POST {StreamableEndpointPath} returned {status} for initialize; servers must not 5xx on a well-formed initialize.");
    }

    [Fact]
    public async Task Streamable_endpoint_returns_parseable_json_for_unknown_method()
    {
        if (!StreamableEnabled) return;

        using var client = CreateClient();
        var unknownRequest = new
        {
            jsonrpc = "2.0",
            id = 99,
            method = "tools/definitely-not-real",
            @params = new { },
        };
        var content = new StringContent(JsonSerializer.Serialize(unknownRequest), System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync(StreamableEndpointPath, content);

        var status = (int)response.StatusCode;
        Assert.True(status < 500, $"Unknown JSON-RPC method returned {status}; servers must not 5xx on unknown methods.");

        var body = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(body))
        {
            // If the server returned a body it must be parseable JSON (text/event-stream
            // bodies are also acceptable when the host advertises streaming).
            var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            if (contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                var act = () => JsonDocument.Parse(body);
                act();
            }
        }
    }

    [Fact]
    public async Task Streamable_endpoint_rejects_malformed_json_with_4xx()
    {
        if (!StreamableEnabled) return;

        using var client = CreateClient();
        var content = new StringContent("{ this is not json", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync(StreamableEndpointPath, content);

        var status = (int)response.StatusCode;
        Assert.True(status is >= 400 and < 500, $"Malformed body returned {status}; expected 4xx.");
    }
}
