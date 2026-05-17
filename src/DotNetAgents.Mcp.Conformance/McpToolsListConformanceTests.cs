using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace DotNetAgents.Mcp.Conformance;

/// <summary>
/// Standard conformance suite for a DNA service's <c>GET /mcp/tools</c> endpoint.
/// Story f34c78e1. Service test projects inherit from this class; xUnit discovers
/// the <c>[Fact]</c> methods on derived classes so no additional wiring is needed
/// beyond providing the entry-point type.
///
/// Asserts the minimal contract every DNA service must honor:
///   - 200 response with a JSON array of tool definitions.
///   - Each tool has camelCase <c>name</c>, <c>description</c>, <c>category</c>, <c>serviceName</c>, <c>inputSchema</c>.
///   - <c>inputSchema.type</c> is present and non-empty.
///   - Every tool's serviceName matches <see cref="ExpectedServiceName"/>.
///   - No duplicate tool names in the response.
/// </summary>
public abstract class McpToolsListConformanceTests<TProgram> : IClassFixture<WebApplicationFactory<TProgram>>
    where TProgram : class
{
    protected WebApplicationFactory<TProgram> Factory { get; }

    protected McpToolsListConformanceTests(WebApplicationFactory<TProgram> factory) => Factory = factory;

    /// <summary>Override to point the harness at your service's <c>serviceName</c>.</summary>
    protected abstract string ExpectedServiceName { get; }

    /// <summary>Override to customize the endpoint path. Defaults to <c>/mcp/tools</c>.</summary>
    protected virtual string ToolsEndpointPath => "/mcp/tools";

    /// <summary>Override for factories that need custom host setup (Production environment, InMemory DB, etc.).</summary>
    protected virtual HttpClient CreateClient() => Factory.CreateClient();

    [Fact]
    public async Task Tools_endpoint_returns_success_status()
    {
        using var client = CreateClient();
        var response = await client.GetAsync(ToolsEndpointPath);
        Assert.True(response.IsSuccessStatusCode, $"GET {ToolsEndpointPath} returned {(int)response.StatusCode}.");
    }

    [Fact]
    public async Task Tools_response_is_array_or_envelope_with_tools_array()
    {
        using var client = CreateClient();
        var response = await client.GetAsync(ToolsEndpointPath);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var tools = ResolveToolsArray(doc.RootElement);
        Assert.Equal(JsonValueKind.Array, tools.ValueKind);
    }

    [Fact]
    public async Task Tools_have_required_camelCase_fields()
    {
        using var client = CreateClient();
        var response = await client.GetAsync(ToolsEndpointPath);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        foreach (var tool in ResolveToolsArray(doc.RootElement).EnumerateArray())
        {
            AssertStringField(tool, "name");
            AssertStringField(tool, "description");
            AssertStringField(tool, "category");
            AssertStringField(tool, "serviceName");
            Assert.True(tool.TryGetProperty("inputSchema", out var schema), $"Tool missing inputSchema: {tool}");
            Assert.True(schema.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(type.GetString()),
                $"Tool inputSchema missing or empty 'type' field: {tool}");
        }
    }

    [Fact]
    public async Task Tools_all_belong_to_expected_service()
    {
        using var client = CreateClient();
        var response = await client.GetAsync(ToolsEndpointPath);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        foreach (var tool in ResolveToolsArray(doc.RootElement).EnumerateArray())
        {
            var svc = tool.GetProperty("serviceName").GetString();
            Assert.Equal(ExpectedServiceName, svc);
        }
    }

    [Fact]
    public async Task Tool_names_are_unique_within_the_service()
    {
        using var client = CreateClient();
        var response = await client.GetAsync(ToolsEndpointPath);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tool in ResolveToolsArray(doc.RootElement).EnumerateArray())
        {
            var name = tool.GetProperty("name").GetString()!;
            Assert.True(names.Add(name), $"Duplicate tool name '{name}' in {ExpectedServiceName} tools list.");
        }
    }

    /// <summary>
    /// Resolve the array of tools from either the bare-array response shape
    /// (legacy) or the McpListToolsResponse envelope shape (current):
    ///   bare:     [ { ... }, { ... } ]
    ///   envelope: { "tools": [ { ... }, { ... } ], "totalCount": N }
    /// </summary>
    private static JsonElement ResolveToolsArray(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array) return root;
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("tools", out var tools) && tools.ValueKind == JsonValueKind.Array)
            return tools;
        // Fall through: return the root so the caller's EnumerateArray() throws
        // a clear exception in tests.
        return root;
    }

    private static void AssertStringField(JsonElement tool, string fieldName)
    {
        Assert.True(tool.TryGetProperty(fieldName, out var field), $"Tool missing '{fieldName}': {tool}");
        Assert.Equal(JsonValueKind.String, field.ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(field.GetString()), $"Tool '{fieldName}' is blank: {tool}");
    }
}
