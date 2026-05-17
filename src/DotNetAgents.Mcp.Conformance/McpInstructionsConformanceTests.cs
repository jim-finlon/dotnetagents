using System.Net.Http.Json;
using System.Text.Json;
using DotNetAgents.Mcp.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace DotNetAgents.Mcp.Conformance;

/// <summary>
/// Standard conformance suite for a DNA service's <c>GET /mcp/instructions</c> endpoint.
/// Service test projects inherit from this class to inherit the test cases — xUnit discovers
/// <c>[Fact]</c> methods on derived classes, so no additional wiring is needed beyond
/// providing the entry-point type and overriding <see cref="ExpectedServiceName"/>.
/// </summary>
/// <typeparam name="TProgram">The service's <c>Program</c> class (or any type in its
/// entry-point assembly) used by <see cref="WebApplicationFactory{TEntryPoint}"/> to host the
/// service in-process.</typeparam>
public abstract class McpInstructionsConformanceTests<TProgram>
    : IClassFixture<WebApplicationFactory<TProgram>>
    where TProgram : class
{
    private static readonly string[] _requiredCoreFields =
    {
        "serviceName",
        "description",
        "bootstrapStep"
    };

    /// <summary>The <see cref="WebApplicationFactory{TEntryPoint}"/> fixture provided by xUnit.</summary>
    protected WebApplicationFactory<TProgram> Factory { get; }

    /// <summary>Initializes a new instance of the <see cref="McpInstructionsConformanceTests{TProgram}"/> class.</summary>
    /// <param name="factory">The web application factory fixture.</param>
    protected McpInstructionsConformanceTests(WebApplicationFactory<TProgram> factory)
        => Factory = factory;

    /// <summary>
    /// The expected snake_case service name the endpoint should return (e.g. "hive_mind").
    /// Must be overridden by each derived class.
    /// </summary>
    protected abstract string ExpectedServiceName { get; }

    /// <summary>
    /// Extension keys the service is expected to emit flat at the top level of the bootstrap
    /// payload (via <see cref="McpInstructionsResponse.Extensions"/>/<c>JsonExtensionData</c>).
    /// Leave empty for services that only emit the shared core fields.
    /// </summary>
    protected virtual IReadOnlyCollection<string> ExpectedExtensionKeys => Array.Empty<string>();

    /// <summary>
    /// Optional expected prompt composition mode. Leave <c>null</c> for services that do not yet
    /// publish prompt composition metadata or do not compose model prompts.
    /// </summary>
    protected virtual string? ExpectedCompositionMode => null;

    /// <summary>
    /// Optional prompt fragment identifiers expected when <see cref="ExpectedCompositionMode"/>
    /// is set. Fragment ids are non-secret ids/versions only; not prompt bodies.
    /// </summary>
    protected virtual IReadOnlyCollection<string> ExpectedPromptFragmentIds => Array.Empty<string>();

    /// <summary>
    /// Builds the <see cref="HttpClient"/> used to call the service. Override to customize
    /// (e.g. to attach an API-key header for services that require auth on this route).
    /// </summary>
    protected virtual HttpClient CreateClient() => Factory.CreateClient();

    /// <summary>Path to the bootstrap endpoint. Default <c>/mcp/instructions</c>.</summary>
    protected virtual string InstructionsPath => "/mcp/instructions";

    [Fact]
    public async Task Instructions_endpoint_returns_success_status()
    {
        using var client = CreateClient();
        var response = await client.GetAsync(InstructionsPath);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Instructions_response_contains_required_core_fields_in_camelCase()
    {
        using var client = CreateClient();
        var response = await client.GetAsync(InstructionsPath);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        foreach (var field in _requiredCoreFields)
        {
            Assert.True(
                json.TryGetProperty(field, out var value),
                $"required core field '{field}' missing from /mcp/instructions response");
            Assert.False(
                value.ValueKind == JsonValueKind.Null || string.IsNullOrWhiteSpace(value.GetString()),
                $"required core field '{field}' must be a non-empty string");
        }

        Assert.Equal(ExpectedServiceName, json.GetProperty("serviceName").GetString());
    }

    [Fact]
    public async Task Instructions_response_deserializes_cleanly_into_shared_typed_model()
    {
        using var client = CreateClient();
        var response = await client.GetAsync(InstructionsPath);
        response.EnsureSuccessStatusCode();

        var typed = await response.Content.ReadFromJsonAsync<McpInstructionsResponse>();

        Assert.NotNull(typed);
        Assert.Equal(ExpectedServiceName, typed!.ServiceName);
        Assert.False(string.IsNullOrWhiteSpace(typed.Description));
        Assert.False(string.IsNullOrWhiteSpace(typed.BootstrapStep));
    }

    [Fact]
    public async Task Instructions_response_never_emits_nested_extensions_key()
    {
        using var client = CreateClient();
        var response = await client.GetAsync(InstructionsPath);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // JsonExtensionData on McpInstructionsResponse.Extensions flattens extras to top level.
        // A literal "extensions" key would indicate the model was bypassed or a prior nested
        // shape leaked through.
        Assert.False(
            json.TryGetProperty("extensions", out _),
            "response must not emit a literal 'extensions' key — extras should serialize flat at top level via JsonExtensionData");
    }

    [Fact]
    public async Task Instructions_response_emits_every_expected_extension_key_at_top_level()
    {
        if (ExpectedExtensionKeys.Count == 0)
            return;

        using var client = CreateClient();
        var response = await client.GetAsync(InstructionsPath);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        foreach (var key in ExpectedExtensionKeys)
        {
            Assert.True(
                json.TryGetProperty(key, out _),
                $"expected extension key '{key}' missing from top-level response (should be flat-serialized via JsonExtensionData)");
        }
    }

    [Fact]
    public async Task Instructions_response_emits_valid_prompt_composition_metadata_when_expected()
    {
        if (ExpectedCompositionMode is null && ExpectedPromptFragmentIds.Count == 0)
            return;

        using var client = CreateClient();
        var response = await client.GetAsync(InstructionsPath);
        response.EnsureSuccessStatusCode();
        var typed = await response.Content.ReadFromJsonAsync<McpInstructionsResponse>();

        Assert.NotNull(typed);
        Assert.Equal(ExpectedCompositionMode, typed!.CompositionMode);
        Assert.True(
            typed.CompositionMode is null || McpPromptCompositionModes.All.Contains(typed.CompositionMode),
            $"compositionMode '{typed.CompositionMode}' is not a standard MCP prompt composition mode");

        foreach (var fragmentId in ExpectedPromptFragmentIds)
        {
            Assert.NotNull(typed.PromptFragmentIds);
            Assert.Contains(fragmentId, typed.PromptFragmentIds!);
        }

        if (typed.PromptFragmentIds is not null)
        {
            Assert.All(typed.PromptFragmentIds, id => Assert.False(string.IsNullOrWhiteSpace(id)));
        }
    }
}
