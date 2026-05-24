// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Nodes;
using DotNetAgents.Mcp.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Mcp.Server;

/// <summary>
/// Maps MCP **Streamable HTTP** transport at a single path (POST/GET/DELETE <c>/mcp</c> by default) per
/// <see href="https://modelcontextprotocol.io/specification/2025-11-25/basic/transports">MCP 2025-11-25</see>.
/// Bridges JSON-RPC methods (<c>initialize</c>, <c>tools/list</c>, <c>tools/call</c>, …) to <see cref="IMcpToolProvider"/>.
/// DNA REST endpoints (<c>GET /mcp/tools</c>, <c>POST /mcp/tools/call</c>) remain unchanged for JARVIS.
/// </summary>
public static class McpStreamableHttpExtensions
{
    private const string ProtocolVersion = "2025-11-25";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Maps Streamable HTTP MCP at <paramref name="routePrefix"/> (default <c>/mcp</c>): POST for JSON-RPC, GET for SSE transport, DELETE for session teardown.
    /// </summary>
    public static IEndpointRouteBuilder MapMcpStreamableHttp(
        this IEndpointRouteBuilder endpoints,
        string serviceName,
        string serverDisplayName,
        string serverVersion,
        string routePrefix = "/mcp")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(serverDisplayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(serverVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(routePrefix);

        // Blazor / antiforgery hosts (e.g. EcosystemAgent) require this or POST /mcp fails for Cursor clients.
        endpoints.MapPost(routePrefix, HandlePostAsync)
            .DisableAntiforgery()
            .WithName("McpStreamableHttpPost")
            .WithTags("MCP", "StreamableHTTP")
            .Accepts<JsonDocument>(MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status202Accepted)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden);

        endpoints.MapMethods(routePrefix, ["OPTIONS"], (HttpContext http) =>
            {
                if (!McpStreamableHttpCorsPolicy.ValidateOrigin(http))
                    return Results.Json(McpStreamableHttpPayloads.BuildCorsForbiddenPayload(serviceName), JsonOptions, statusCode: StatusCodes.Status403Forbidden);
                McpStreamableHttpCorsPolicy.ApplyCursorCorsHeaders(http);
                return Results.StatusCode(StatusCodes.Status204NoContent);
            })
            .DisableAntiforgery()
            .WithName("McpStreamableHttpOptions")
            .WithTags("MCP", "StreamableHTTP");

        endpoints.MapGet(routePrefix, HandleGetAsync)
            .DisableAntiforgery()
            .WithName("McpStreamableHttpGet")
            .WithTags("MCP", "StreamableHTTP");

        endpoints.MapDelete(routePrefix, (HttpContext http) =>
            {
                var sessionId = McpStreamableHttpSessions.ResolveSessionId(http, createIfMissing: false);
                McpStreamableHttpCorsPolicy.ApplyCursorCorsHeaders(http);
                if (!McpStreamableHttpCorsPolicy.ValidateOrigin(http))
                    return Results.Json(McpStreamableHttpPayloads.BuildCorsForbiddenPayload(serviceName), JsonOptions, statusCode: StatusCodes.Status403Forbidden);
                McpStreamableHttpSessions.RemoveSession(sessionId);
                return Results.StatusCode(StatusCodes.Status204NoContent);
            })
            .DisableAntiforgery()
            .WithName("McpStreamableHttpDelete")
            .WithTags("MCP", "StreamableHTTP");

        // Cursor 3.x probes /.well-known/oauth-* before Streamable HTTP. A plain ASP.NET 404 body breaks its OAuth JSON
        // parser ("Invalid OAuth error response"). 401 + JSON lets the client fall back to X-Api-Key headers in mcp.json.
        // Registered here so every host that calls MapMcpStreamableHttp gets this without a separate middleware call.
        // Story 28b87b91: skip the auth-server compat stub when the November-2025 PKCE adapter is wired —
        // McpDiscoveryEndpointExtensions maps the real RFC 8414 metadata at the same path and would otherwise
        // collide. Detect via IOptions<McpAuthHostingOptions> registration: when present, the host called
        // AddMcpAuthServer + MapMcpAuth and owns the well-known route.
        // Story 4aa4e4fe: same gating now applies to /.well-known/oauth-protected-resource — McpAuth
        // ships the real RFC 9728 metadata document, and clients (Cursor / Claude Code) need a 200,
        // not a 401, on this endpoint. Without the gate, the legacy 401 stub here used to win and
        // every Core 4 service answered 401 — which surfaced as a misleading "OAuth error" in /mcp.
        var hasMcpAuth = endpoints.ServiceProvider.GetService<DotNetAgents.Mcp.Server.Authentication.McpAuthEnabledMarker>() is not null;
        if (!hasMcpAuth)
        {
            endpoints.MapGet("/.well-known/oauth-authorization-server", static () =>
                    Results.Json(
                        McpStreamableHttpPayloads.BuildOAuthProbePayload("mcp", "/.well-known/oauth-authorization-server"),
                        JsonOptions,
                        statusCode: StatusCodes.Status401Unauthorized))
                .WithName("McpCursorOAuthProbeAuthServer")
                .WithTags("MCP", "CursorCompatibility");
            // Cursor 3.x may GET the advertised authorization URL even when the workstation uses
            // X-Api-Key. ASP.NET's default empty 404 body breaks OAuth JSON parsing ("Invalid OAuth error
            // response"). Return explicit JSON so the client can fall back to mcp.json headers.
            endpoints.MapGet("/.mcp/oauth/authorize", static () =>
                    Results.Json(
                        new
                        {
                            error = "authorization_not_supported",
                            error_description =
                                "DNA LAN MCP expects X-Api-Key on POST /mcp. Interactive browser OAuth is not enabled on this host.",
                            errorCode = "AUTHORIZATION_NOT_SUPPORTED",
                            remediation = McpStreamableHttpPayloads.BuildRemediation(
                                "auth",
                                "mcp",
                                ".mcp.oauth.authorize",
                                "AUTHORIZATION_NOT_SUPPORTED",
                                null,
                                "Configure X-Api-Key from credential resolver or the local MCP client environment, then retry POST /mcp.",
                                ["get_instructions", "retry_with_x_api_key"],
                                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                                {
                                    ["path"] = "/.mcp/oauth/authorize",
                                    ["expectedHeader"] = "X-Api-Key"
                                })
                        },
                        statusCode: StatusCodes.Status400BadRequest))
                .WithName("McpCursorOAuthAuthorizeCompatibilityLegacy")
                .WithTags("MCP", "CursorCompatibility");
            endpoints.MapGet("/.well-known/oauth-protected-resource", static () =>
                    Results.Json(
                        McpStreamableHttpPayloads.BuildOAuthProbePayload("mcp", "/.well-known/oauth-protected-resource"),
                        JsonOptions,
                        statusCode: StatusCodes.Status401Unauthorized))
                .WithName("McpCursorOAuthProbeProtectedResource")
                .WithTags("MCP", "CursorCompatibility");
        }

        return endpoints;

        async Task HandleGetAsync(HttpContext http, CancellationToken cancellationToken)
        {
            McpStreamableHttpCorsPolicy.ApplyCursorCorsHeaders(http);
            if (!McpStreamableHttpCorsPolicy.ValidateOrigin(http))
            {
                http.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            var acceptsStream = http.Request.Headers.Accept
                .Any(a => a?.Contains("text/event-stream", StringComparison.OrdinalIgnoreCase) == true);
            if (!acceptsStream)
            {
                http.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                return;
            }

            var sessionId = McpStreamableHttpSessions.ResolveSessionId(http, createIfMissing: true);
            http.Response.StatusCode = StatusCodes.Status200OK;
            http.Response.Headers["MCP-Session-Id"] = sessionId;
            http.Response.Headers["Cache-Control"] = "no-cache";
            http.Response.Headers["Connection"] = "keep-alive";
            http.Response.ContentType = "text/event-stream";
            await http.Response.WriteAsync($": session={sessionId}\n\n", cancellationToken).ConfigureAwait(false);
            await http.Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(false);
                    await http.Response.WriteAsync(": keepalive\n\n", cancellationToken).ConfigureAwait(false);
                    await http.Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Request aborted by client.
            }
        }

        async Task<IResult> HandlePostAsync(
            HttpContext http,
            IMcpToolProvider provider,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken)
        {
            var requestSessionId = McpStreamableHttpSessions.ResolveSessionId(http, createIfMissing: false);
            if (!string.IsNullOrWhiteSpace(requestSessionId))
                http.Response.Headers["MCP-Session-Id"] = requestSessionId;

            McpStreamableHttpCorsPolicy.ApplyCursorCorsHeaders(http);
            if (!McpStreamableHttpCorsPolicy.ValidateOrigin(http))
                return Results.Json(McpStreamableHttpPayloads.BuildCorsForbiddenPayload(serviceName), JsonOptions, statusCode: StatusCodes.Status403Forbidden);

            JsonDocument doc;
            try
            {
                doc = await JsonDocument.ParseAsync(http.Request.Body, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (JsonException)
            {
                return McpStreamableHttpJsonRpc.BadRequest(null, -32700, "Parse error", serviceName, JsonOptions);
            }

            using (doc)
            {
                var root = doc.RootElement;
                if (!root.TryGetProperty("jsonrpc", out var jv) || jv.GetString() != "2.0")
                    return McpStreamableHttpJsonRpc.BadRequest(McpStreamableHttpJsonRpc.GetRequestId(root), -32600, "Invalid JSON-RPC", serviceName, JsonOptions);

                if (!root.TryGetProperty("method", out var methodEl))
                    return McpStreamableHttpJsonRpc.BadRequest(McpStreamableHttpJsonRpc.GetRequestId(root), -32600, "Missing method", serviceName, JsonOptions);

                var method = methodEl.GetString();
                if (string.IsNullOrEmpty(method))
                    return McpStreamableHttpJsonRpc.BadRequest(McpStreamableHttpJsonRpc.GetRequestId(root), -32600, "Invalid method", serviceName, JsonOptions);

                var hasId = root.TryGetProperty("id", out var idElement);
                JsonElement? id = hasId ? idElement : null;

                // Notifications (no id): 202 Accepted, empty body
                if (!hasId)
                {
                    if (method == "notifications/initialized" || method.StartsWith("notifications/", StringComparison.Ordinal))
                        return Results.StatusCode(StatusCodes.Status202Accepted);
                    return McpStreamableHttpJsonRpc.BadRequest(null, -32600, "Invalid Request: missing id for non-notification", serviceName, JsonOptions);
                }

                root.TryGetProperty("params", out var paramsElement);

                var reqId = id!.Value;
                var lessonPublisher = http.RequestServices.GetService<ILessonEventPublisher>()
                    ?? NoOpLessonEventPublisher.Instance;
                var observabilityPublisher = http.RequestServices.GetService<IDnaObservabilityPublisher>()
                    ?? NoOpDnaObservabilityPublisher.Instance;
                return method switch
                {
                    "initialize" => await HandleInitializeAsync(http, reqId, paramsElement, cancellationToken).ConfigureAwait(false),
                    "ping" => McpStreamableHttpJsonRpc.Result(reqId, new JsonObject(), JsonOptions),
                    "tools/list" => await HandleToolsListAsync(reqId, paramsElement, provider, serviceName, cancellationToken).ConfigureAwait(false),
                    "tools/call" => await HandleToolsCallAsync(reqId, paramsElement, provider, serviceName, loggerFactory, lessonPublisher, observabilityPublisher, cancellationToken).ConfigureAwait(false),
                    _ => McpStreamableHttpJsonRpc.Error(reqId, -32601, $"Method not found: {method}", serviceName, JsonOptions)
                };
            }
        }

        Task<IResult> HandleInitializeAsync(HttpContext http, JsonElement id, JsonElement paramsElement, CancellationToken ct)
        {
            _ = paramsElement;
            _ = ct;
            var sessionId = McpStreamableHttpSessions.ResolveSessionId(http, createIfMissing: true);
            http.Response.Headers["MCP-Session-Id"] = sessionId;

            var result = new JsonObject
            {
                ["protocolVersion"] = ProtocolVersion,
                ["capabilities"] = new JsonObject
                {
                    // MCP 2025-11-25: servers that expose tools MUST declare this; empty {} breaks strict clients (Cursor lists no tools).
                    ["tools"] = new JsonObject { ["listChanged"] = false }
                },
                ["serverInfo"] = new JsonObject
                {
                    ["name"] = serverDisplayName,
                    ["version"] = serverVersion
                },
                ["instructions"] = $"DNA agent ({serviceName}). Use tools/list then tools/call. Legacy REST: GET /mcp/tools, POST /mcp/tools/call."
            };

            return Task.FromResult(McpStreamableHttpJsonRpc.Result(id, result, JsonOptions));
        }

        async Task<IResult> HandleToolsListAsync(
            JsonElement id,
            JsonElement paramsElement,
            IMcpToolProvider provider,
            string svc,
            CancellationToken ct)
        {
            var tools = await provider.GetToolsAsync(svc, ct).ConfigureAwait(false);
            var tagged = tools.Select(t => t.ServiceName == null ? t with { ServiceName = svc } : t).ToList();

            var arr = new JsonArray();
            foreach (var t in tagged)
            {
                arr.Add(McpStreamableHttpToolProjection.BuildMcpToolJson(t, JsonOptions));
            }

            var result = new JsonObject { ["tools"] = arr };
            return McpStreamableHttpJsonRpc.Result(id, result, JsonOptions);
        }

        async Task<IResult> HandleToolsCallAsync(
            JsonElement id,
            JsonElement paramsElement,
            IMcpToolProvider provider,
            string svc,
            ILoggerFactory loggerFactory,
            ILessonEventPublisher lessonPublisher,
            IDnaObservabilityPublisher observabilityPublisher,
            CancellationToken ct)
        {
            if (paramsElement.ValueKind == JsonValueKind.Undefined ||
                !paramsElement.TryGetProperty("name", out var nameEl))
                return McpStreamableHttpJsonRpc.Error(id, -32602, "Invalid params: tools/call requires name", serviceName, JsonOptions);

            var toolName = nameEl.GetString();
            if (string.IsNullOrWhiteSpace(toolName))
                return McpStreamableHttpJsonRpc.Error(id, -32602, "Invalid params: name", serviceName, JsonOptions);

            // Some clients still send dotted display names (e.g. credentials.get_credential). Normalize
            // to the framework-enforced [A-Za-z0-9_] form so a call routes to the tool registered
            // under the canonical name. New services register valid names directly — this path exists
            // only to keep older clients working during transition.
            toolName = McpToolNameConvention.Normalize(toolName);

            var args = new Dictionary<string, object>();
            if (paramsElement.TryGetProperty("arguments", out var argsEl) && argsEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in argsEl.EnumerateObject())
                    args[p.Name] = McpStreamableHttpToolProjection.JsonElementToArg(p.Value);
            }

            var logger = loggerFactory.CreateLogger("McpStreamableHttp");
            var stopwatch = Stopwatch.StartNew();
            var correlationId = Guid.NewGuid().ToString("N");
            try
            {
                var catalog = await provider.GetToolsAsync(svc, ct).ConfigureAwait(false);
                var definition = catalog.FirstOrDefault(t =>
                    string.Equals(t.Name, toolName, StringComparison.OrdinalIgnoreCase));
                var missingArgs = McpRequiredArgumentGuard.TryGetMissingArgumentResponse(
                    definition,
                    toolName!,
                    args);
                var response = missingArgs
                    ?? await provider.CallToolAsync(toolName!, args, ct).ConfigureAwait(false);
                stopwatch.Stop();
                var callResult = McpStreamableHttpToolProjection.McpToCallToolResult(response, JsonOptions);
                await McpStreamableHttpLearningPublisher.PublishForToolCallAsync(
                    lessonPublisher,
                    svc,
                    toolName!,
                    correlationId,
                    response,
                    stopwatch.ElapsedMilliseconds,
                    ct).ConfigureAwait(false);
                await McpObservabilityPublisher.PublishToolCallAsync(
                    observabilityPublisher,
                    svc,
                    toolName!,
                    correlationId,
                    args,
                    response,
                    ct).ConfigureAwait(false);
                return McpStreamableHttpJsonRpc.Result(id, JsonSerializer.SerializeToNode(callResult, JsonOptions), JsonOptions);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                logger.LogError(ex, "tools/call failed for {Tool}", toolName);
                var errResult = new CallToolResultDto
                {
                    Content =
                    [
                        new ContentBlockDto { Type = "text", Text = ex.Message }
                    ],
                    IsError = true
                };
                var exceptionResponse = new McpToolCallResponse
                {
                    Success = false,
                    Error = ex.Message,
                    ErrorCode = "TOOL_ERROR",
                    DurationMs = stopwatch.ElapsedMilliseconds
                };
                await McpStreamableHttpLearningPublisher.PublishForToolCallAsync(
                    lessonPublisher,
                    svc,
                    toolName!,
                    correlationId,
                    exceptionResponse,
                    stopwatch.ElapsedMilliseconds,
                    CancellationToken.None).ConfigureAwait(false);
                await McpObservabilityPublisher.PublishToolCallAsync(
                    observabilityPublisher,
                    svc,
                    toolName!,
                    correlationId,
                    args,
                    exceptionResponse,
                    CancellationToken.None).ConfigureAwait(false);
                return McpStreamableHttpJsonRpc.Result(id, JsonSerializer.SerializeToNode(errResult, JsonOptions), JsonOptions);
            }
        }
    }

    /// <summary>
    /// Run <b>before</b> other middleware. Cursor 3.x sends <c>GET /.well-known/oauth-*</c> before Streamable HTTP; a
    /// default ASP.NET 404 body breaks its OAuth parser. Responds with <c>401</c> and <c>{}</c> JSON so the client
    /// can fall back to API keys. (Also registered as routes inside <see cref="MapMcpStreamableHttp"/>; this middleware
    /// guarantees order on hosts that need probes handled before auth/logging.)
    /// </summary>
    /// <remarks>
    /// Story 28b87b91: when the host has registered <see cref="DotNetAgents.Mcp.Server.Authentication.McpAuthServerExtensions.AddMcpAuthServer"/>,
    /// this middleware stands down for the <c>oauth-authorization-server</c> path so the real RFC 8414 metadata
    /// from <see cref="DotNetAgents.Mcp.Server.Authentication.McpDiscoveryEndpointExtensions"/> can serve the
    /// route instead of a Cursor 3.x compatibility 401.
    /// </remarks>
    public static IApplicationBuilder UseMcpCursorWellKnownProbeFirst(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var hasMcpAuth = app.ApplicationServices.GetService<DotNetAgents.Mcp.Server.Authentication.McpAuthEnabledMarker>() is not null;

        return app.Use(async (context, next) =>
        {
            var path = context.Request.Path.Value ?? string.Empty;
            if (HttpMethods.IsGet(context.Request.Method) &&
                path.StartsWith("/.well-known/oauth", StringComparison.OrdinalIgnoreCase))
            {
                // Story 4aa4e4fe: when the November-2025 PKCE adapter is wired, defer to the real
                // endpoints for both the RFC 8414 authorization-server metadata and the RFC 9728
                // protected-resource metadata. Without this, the legacy 401 stub here would mask
                // the real metadata documents and MCP clients (Cursor / Claude Code) would render
                // a misleading "OAuth error" instead of the right API-key challenge.
                var deferToPkceAdapter = hasMcpAuth &&
                    DotNetAgents.Mcp.Server.Authentication.McpAuthChallengeWriter.IsPublicMcpDiscoveryPath(context.Request.Path);

                if (!deferToPkceAdapter)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = MediaTypeNames.Application.Json;
                    await context.Response.WriteAsJsonAsync(
                        McpStreamableHttpPayloads.BuildOAuthProbePayload("mcp", path),
                        JsonOptions,
                        context.RequestAborted).ConfigureAwait(false);
                    return;
                }
            }

            await next().ConfigureAwait(false);
        });
    }

    /// <summary>
    /// CORS for <c>/mcp</c> paths used by Cursor desktop / browser clients. OAuth probe responses are registered in
    /// <see cref="MapMcpStreamableHttp"/> so all Streamable HTTP hosts get them. Call early in the pipeline.
    /// </summary>
    public static IApplicationBuilder UseCursorMcpCors(this IApplicationBuilder app)
    {
        return app.Use(async (http, next) =>
        {
            var path = http.Request.Path.Value ?? string.Empty;
            if (!path.StartsWith("/mcp", StringComparison.OrdinalIgnoreCase))
            {
                await next().ConfigureAwait(false);
                return;
            }

            if (!McpStreamableHttpCorsPolicy.ValidateOrigin(http))
            {
                http.Response.StatusCode = StatusCodes.Status403Forbidden;
                http.Response.ContentType = MediaTypeNames.Application.Json;
                await http.Response.WriteAsJsonAsync(
                    McpStreamableHttpPayloads.BuildCorsForbiddenPayload("mcp"),
                    JsonOptions,
                    http.RequestAborted).ConfigureAwait(false);
                return;
            }

            if (HttpMethods.IsOptions(http.Request.Method) &&
                http.Request.Headers.ContainsKey("Access-Control-Request-Method"))
            {
                McpStreamableHttpCorsPolicy.ApplyCursorCorsHeaders(http);
                http.Response.StatusCode = StatusCodes.Status204NoContent;
                return;
            }

            McpStreamableHttpCorsPolicy.ApplyCursorCorsHeaders(http);
            await next().ConfigureAwait(false);
        });
    }

}
