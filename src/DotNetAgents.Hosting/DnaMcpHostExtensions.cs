using DotNetAgents.Mcp.Models;
using DotNetAgents.Mcp.Server;
using DotNetAgents.Mcp.Server.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DotNetAgents.Hosting;

/// <summary>
/// MCP-specific host-profile helpers for DNA ASP.NET Core services.
/// </summary>
public static class DnaMcpHostExtensions
{
    /// <summary>
    /// Registers the shared MCP host profile: options, lifecycle hooks, tool registry, and
    /// November 2025 auth-mode binding. Service-owned tool providers remain service-local.
    /// </summary>
    public static IServiceCollection AddDnaMcpHost(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DnaMcpHostOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(DnaMcpHostOptions.SectionPath);
        services.AddOptions<DnaMcpHostOptions>()
            .Bind(section)
            .PostConfigure(options =>
            {
                configure?.Invoke(options);
                ApplyDefaults(options);
            })
            .ValidateDataAnnotations();

        var snapshot = ResolveSnapshot(section, configure);
        services.AddMcpLifecycleHooks();
        services.AddUnifiedToolRegistry();
        services.AddMcpAuthServer(configuration);

        var authSectionPath = string.IsNullOrWhiteSpace(snapshot.AuthModeSection)
            ? "DotNetAgents:Mcp:Server:Auth"
            : snapshot.AuthModeSection;
        if (!string.Equals(authSectionPath, McpAuthHostingOptions.SectionName, StringComparison.Ordinal))
            services.Configure<McpAuthHostingOptions>(configuration.GetSection(authSectionPath));

        return services;
    }

    /// <summary>
    /// Maps the configured MCP Streamable HTTP route, auth endpoints, and generic legacy REST
    /// MCP routes using the registered <see cref="IMcpToolProvider"/>.
    /// </summary>
    public static IEndpointRouteBuilder MapDnaMcpHost(this IEndpointRouteBuilder endpoints)
        => endpoints.MapDnaMcpHost(mapInstructions: true, mapLegacyRest: true);

    /// <summary>
    /// Maps the configured MCP host profile while allowing services with bespoke legacy REST
    /// behavior to provide their own parity-preserving mapping delegates.
    /// </summary>
    public static IEndpointRouteBuilder MapDnaMcpHost(
        this IEndpointRouteBuilder endpoints,
        Action<IEndpointRouteBuilder, DnaMcpHostOptions>? mapInstructions,
        Action<IEndpointRouteBuilder, DnaMcpHostOptions>? mapLegacyRest)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider
            .GetRequiredService<IOptions<DnaMcpHostOptions>>()
            .Value;

        endpoints.MapMcpStreamableHttp(
            options.ServiceName,
            options.ServerDisplayName,
            options.ServerVersion,
            NormalizePath(options.StreamableHttpPath, "/mcp"));

        mapInstructions?.Invoke(endpoints, options);
        mapLegacyRest?.Invoke(endpoints, options);

        endpoints.MapMcpAuth(options.ServerDisplayName);
        return endpoints;
    }

    /// <summary>
    /// Maps the configured MCP host profile and conditionally maps generic instructions and
    /// legacy REST endpoints. This overload is useful for tests and simple MCP hosts.
    /// </summary>
    public static IEndpointRouteBuilder MapDnaMcpHost(
        this IEndpointRouteBuilder endpoints,
        bool mapInstructions,
        bool mapLegacyRest,
        McpInstructionsResponse? instructionsBootstrap = null)
    {
        return endpoints.MapDnaMcpHost(
            mapInstructions
                ? (routes, options) => routes.MapDnaMcpInstructions(options, instructionsBootstrap)
                : null,
            mapLegacyRest
                ? (routes, options) => routes.MapDnaMcpLegacyRest(options)
                : null);
    }

    /// <summary>Maps GET instructions at <see cref="DnaMcpHostOptions.InstructionsPath"/>.</summary>
    public static IEndpointRouteBuilder MapDnaMcpInstructions(
        this IEndpointRouteBuilder endpoints,
        DnaMcpHostOptions options,
        McpInstructionsResponse? instructionsBootstrap)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(options);

        if (instructionsBootstrap is null)
            return endpoints;

        endpoints.MapGet(NormalizePath(options.InstructionsPath, "/mcp/instructions"), () =>
                Results.Json(instructionsBootstrap, DnaMcpJson.JsonOptions))
            .WithName("GetMcpInstructions")
            .WithDisplayName("MCP bootstrap instructions")
            .WithTags("MCP");
        return endpoints;
    }

    /// <summary>Maps generic legacy REST MCP endpoints under <see cref="DnaMcpHostOptions.LegacyRestPath"/>.</summary>
    public static IEndpointRouteBuilder MapDnaMcpLegacyRest(
        this IEndpointRouteBuilder endpoints,
        DnaMcpHostOptions options)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(options);

        var group = endpoints.MapGroup(NormalizePath(options.LegacyRestPath, "/mcp"))
            .WithTags("MCP");

        group.MapGet("/tools", async (string? category, int? limit, IMcpToolProvider provider, CancellationToken ct) =>
            {
                var tools = await provider.GetToolsAsync(options.ServiceName, ct).ConfigureAwait(false);
                var tagged = tools.Select(t => t.ServiceName == null ? t with { ServiceName = options.ServiceName } : t);
                if (!string.IsNullOrWhiteSpace(category))
                    tagged = tagged.Where(t => string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase));

                var list = tagged.ToList();
                if (limit is > 0)
                    list = list.Take(limit.Value).ToList();

                return Results.Json(
                    new McpListToolsResponse { Tools = list, TotalCount = list.Count },
                    DnaMcpJson.JsonOptions);
            })
            .WithName("ListMcpTools")
            .WithDisplayName("List MCP tools")
            .Produces<McpListToolsResponse>(StatusCodes.Status200OK);

        group.MapPost("/tools/call", async (
                McpToolCallRequest request,
                IMcpToolProvider provider,
                CancellationToken ct) =>
            {
                if (request is null || string.IsNullOrWhiteSpace(request.Tool))
                {
                    return Results.BadRequest(new McpToolCallResponse
                    {
                        Success = false,
                        Error = "Tool name is required",
                        ErrorCode = "INVALID_REQUEST"
                    });
                }

                var response = await provider.CallToolAsync(
                    request.Tool,
                    request.Arguments ?? new Dictionary<string, object>(),
                    ct).ConfigureAwait(false);
                return Results.Json(response, DnaMcpJson.JsonOptions);
            })
            .WithName("CallMcpTool")
            .WithDisplayName("Call MCP tool")
            .Produces<McpToolCallResponse>(StatusCodes.Status200OK)
            .Produces<McpToolCallResponse>(StatusCodes.Status400BadRequest);

        return endpoints;
    }

    private static DnaMcpHostOptions ResolveSnapshot(
        IConfigurationSection section,
        Action<DnaMcpHostOptions>? configure)
    {
        var options = new DnaMcpHostOptions();
        section.Bind(options);
        configure?.Invoke(options);
        ApplyDefaults(options);
        return options;
    }

    private static void ApplyDefaults(DnaMcpHostOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ServerDisplayName))
            options.ServerDisplayName = options.ServiceName;

        if (string.IsNullOrWhiteSpace(options.ServerVersion))
            options.ServerVersion = "1.0.0";

        options.StreamableHttpPath = NormalizePath(options.StreamableHttpPath, "/mcp");
        options.LegacyRestPath = NormalizePath(options.LegacyRestPath, "/mcp");
        options.InstructionsPath = NormalizePath(options.InstructionsPath, "/mcp/instructions");
        options.AuthModeSection = string.IsNullOrWhiteSpace(options.AuthModeSection)
            ? "DotNetAgents:Mcp:Server:Auth"
            : options.AuthModeSection;
    }

    private static string NormalizePath(string configured, string fallback)
        => string.IsNullOrWhiteSpace(configured) ? fallback : configured;
}

internal static class DnaMcpJson
{
    public static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
}
