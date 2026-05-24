// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Mcp.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// Inspects <c>Authorization: Bearer</c> headers on /mcp routes and surfaces validated PKCE
/// bearer claims onto <see cref="HttpContext.Items"/> so downstream handlers can read scopes and
/// the resolved principal via <see cref="McpPkceBearerHttpContextExtensions"/>.
/// </summary>
/// <remarks>
/// <para>
/// Behavior depends on <see cref="McpAuthMode"/>:
/// </para>
/// <list type="bullet">
/// <item><see cref="McpAuthMode.Legacy"/>: middleware is a no-op; X-Api-Key is the only auth.</item>
/// <item><see cref="McpAuthMode.Both"/>: a Bearer header is validated when present, but legacy
///   X-Api-Key requests are still accepted by downstream middleware.</item>
/// <item><see cref="McpAuthMode.Strict"/>: a missing Bearer header on /mcp routes returns 401
///   <c>missing_bearer_token</c>; an invalid Bearer returns 401 with the validator's failure reason.</item>
/// </list>
/// <para>
/// Tokens MUST NOT be logged. The middleware never logs the raw Authorization value — only the
/// validator's failure reason (<c>token_expired</c>, <c>invalid_audience</c>, etc.).
/// </para>
/// </remarks>
public sealed class JwtMcpPkceBearerMiddleware
{
    private const string BearerScheme = "Bearer ";
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtMcpPkceBearerMiddleware> _logger;

    public JwtMcpPkceBearerMiddleware(RequestDelegate next, ILogger<JwtMcpPkceBearerMiddleware>? logger = null)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? NullLogger<JwtMcpPkceBearerMiddleware>.Instance;
    }

    public async Task InvokeAsync(HttpContext context, JwtMcpPkceBearerValidator validator, IOptions<McpAuthHostingOptions> hostingOptions)
    {
        var hosting = hostingOptions.Value;
        if (hosting.Mode == McpAuthMode.Legacy || !IsMcpRoute(context.Request))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var auth = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(auth) || !auth.StartsWith(BearerScheme, StringComparison.Ordinal))
        {
            if (hosting.Mode == McpAuthMode.Strict)
            {
                await WriteAuthErrorAsync(context, "missing_bearer_token", "Strict MCP auth requires Authorization: Bearer <jwt>.").ConfigureAwait(false);
                return;
            }

            await _next(context).ConfigureAwait(false);
            return;
        }

        var token = auth[BearerScheme.Length..].Trim();
        var result = await validator.ValidateAsync(token, context.RequestAborted).ConfigureAwait(false);
        if (!result.IsValid)
        {
            _logger.LogDebug("MCP bearer rejected: {Reason}.", result.FailureReason);
            await WriteAuthErrorAsync(context, "invalid_token", result.FailureReason ?? "validation_failed").ConfigureAwait(false);
            return;
        }

        context.Items[McpPkceBearerContextKey] = new McpPkceBearerContext(result.Principal!, result.Scopes, result.ExpiresAtUtc);
        if (context.User?.Identity is null || !context.User.Identity.IsAuthenticated)
        {
            context.User = result.Principal!;
        }

        await _next(context).ConfigureAwait(false);
    }

    private static bool IsMcpRoute(HttpRequest request)
    {
        if (!request.Path.StartsWithSegments("/mcp", out var remaining))
        {
            return false;
        }

        // /mcp/instructions is the public bootstrap surface — same posture as the existing
        // X-Api-Key gate in knowledge-memory service/workflow orchestrator: do not require auth on it.
        return !remaining.Equals("/instructions", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task WriteAuthErrorAsync(HttpContext context, string error, string description)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.WWWAuthenticate = $"Bearer error=\"{error}\"";
        await context.Response.WriteAsJsonAsync(new
        {
            error,
            error_description = description,
        }).ConfigureAwait(false);
    }

    /// <summary>HttpContext.Items key under which <see cref="McpPkceBearerContext"/> is stored.</summary>
    public const string McpPkceBearerContextKey = "DotNetAgents.Mcp.Server.PkceBearerContext";
}

/// <summary>Validated MCP PKCE bearer context surfaced on the request after the middleware runs.</summary>
/// <param name="Principal">The resolved <see cref="System.Security.Claims.ClaimsPrincipal"/>.</param>
/// <param name="Scopes">Allowlisted scopes from the token's <c>mcp_scopes</c> claim.</param>
/// <param name="ExpiresAtUtc">UTC expiry from the token's <c>exp</c> claim.</param>
public sealed record McpPkceBearerContext(System.Security.Claims.ClaimsPrincipal Principal, IReadOnlyList<string> Scopes, DateTimeOffset ExpiresAtUtc);

/// <summary>Helpers for downstream handlers to read the validated MCP bearer context.</summary>
public static class McpPkceBearerHttpContextExtensions
{
    /// <summary>
    /// Returns the validated MCP bearer context if <see cref="JwtMcpPkceBearerMiddleware"/> ran
    /// and accepted a Bearer JWT on this request; otherwise null. Returns null on legacy
    /// X-Api-Key requests in <see cref="McpAuthMode.Both"/>.
    /// </summary>
    public static McpPkceBearerContext? GetMcpPkceBearer(this HttpContext context)
    {
        if (context.Items.TryGetValue(JwtMcpPkceBearerMiddleware.McpPkceBearerContextKey, out var raw) && raw is McpPkceBearerContext ctx)
        {
            return ctx;
        }

        return null;
    }
}

/// <summary>Pipeline registration for <see cref="JwtMcpPkceBearerMiddleware"/>.</summary>
public static class JwtMcpPkceBearerMiddlewareExtensions
{
    /// <summary>
    /// Adds the MCP PKCE bearer validation middleware. Must be called BEFORE the existing
    /// X-Api-Key gate so Strict-mode 401s short-circuit cleanly.
    /// </summary>
    public static IApplicationBuilder UseMcpPkceBearer(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<JwtMcpPkceBearerMiddleware>();
    }
}
