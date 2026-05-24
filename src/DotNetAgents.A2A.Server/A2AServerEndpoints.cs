// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using DotNetAgents.A2A;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetAgents.A2A.Server;

/// <summary>
/// IEndpointRouteBuilder extensions that map the A2A 1.0 server surface onto an ASP.NET Core
/// host: agent card discovery at <c>/.well-known/agent.json</c>, task lifecycle at
/// <c>{BaseRoute}/tasks/send</c> + <c>tasks/sendSubscribe</c> (SSE streaming) +
/// <c>tasks/get</c> + <c>tasks/cancel</c>.
/// </summary>
/// <remarks>
/// Story c46e33de (Phase 2B keystone). The A2A spec at github.com/google-a2a/A2A defines the
/// wire shape; this implementation maps the v0.3 surface onto DotNetAgents.A2A's existing
/// abstractions (<see cref="IA2AAgent"/>, <see cref="IA2AAgentRegistry"/>).
/// <para>
/// Auth model: a per-host bearer-token allowlist via <see cref="A2AServerOptions.AllowedBearerTokens"/>.
/// Production deployments populate the allowlist from CredentialsAgent at startup. Signed
/// Agent Cards (JWT/HTTP Signatures) are deferred to a follow-up story; the current
/// implementation returns the agent card unsigned with the expectation that a reverse-proxy
/// or follow-up signing middleware adds the signature header.
/// </para>
/// </remarks>
public static class A2AServerEndpoints
{
    /// <summary>
    /// Map all A2A 1.0 server endpoints onto the supplied <paramref name="endpoints"/>. Reads
    /// <see cref="A2AServerOptions"/> from DI to construct the route paths.
    /// </summary>
    public static IEndpointConventionBuilder MapA2AServer(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<A2AServerOptions>>().Value;
        var baseRoute = options.BaseRoute.TrimEnd('/');

        var conventions = new EndpointConventionAggregate();

        var agentCardPath = string.IsNullOrWhiteSpace(options.AgentCardPath)
            ? "/.well-known/agent.json"
            : options.AgentCardPath;

        conventions.Add(endpoints.MapGet(agentCardPath, AgentCardHandler)
            .WithName("A2A_AgentCard").WithTags("A2A"));

        conventions.Add(endpoints.MapPost($"{baseRoute}/tasks/send", TaskSendHandler)
            .WithName("A2A_TaskSend").WithTags("A2A"));

        conventions.Add(endpoints.MapPost($"{baseRoute}/tasks/sendSubscribe", TaskSendSubscribeHandler)
            .WithName("A2A_TaskSendSubscribe").WithTags("A2A"));

        conventions.Add(endpoints.MapGet($"{baseRoute}/tasks/{{taskId}}", TaskGetHandler)
            .WithName("A2A_TaskGet").WithTags("A2A"));

        conventions.Add(endpoints.MapPost($"{baseRoute}/tasks/{{taskId}}/cancel", TaskCancelHandler)
            .WithName("A2A_TaskCancel").WithTags("A2A"));

        return conventions;
    }

    private static IResult AgentCardHandler(HttpContext ctx)
    {
        var registry = ctx.RequestServices.GetRequiredService<IA2AAgentRegistry>();
        var options = ctx.RequestServices.GetRequiredService<IOptions<A2AServerOptions>>().Value;
        var agents = registry.List()
            .Select(registry.GetById)
            .Where(agent => agent is not null)
            .Cast<IA2AAgent>()
            .ToList();
        if (agents.Count == 0)
        {
            return Results.NotFound(new { error = "no_agents_registered" });
        }
        return Results.Json(BuildServiceCard(options, agents));
    }

    private static async Task<IResult> TaskSendHandler(HttpContext ctx, A2ATask task)
    {
        if (!IsAuthenticated(ctx, out var authError)) return authError!;

        var registry = ctx.RequestServices.GetRequiredService<IA2AAgentRegistry>();
        var options = ctx.RequestServices.GetRequiredService<IOptions<A2AServerOptions>>().Value;
        if (string.IsNullOrEmpty(task.Skill))
        {
            return Results.BadRequest(new { error = "skill_required" });
        }
        var authorization = await AuthorizeRequestAsync(ctx, task, cancellationToken: ctx.RequestAborted).ConfigureAwait(false);
        if (!authorization.IsAllowed)
        {
            return authorization.ErrorResult ?? Results.Forbid();
        }
        var agent = registry.FindByCapability(task.Skill).FirstOrDefault()
                    ?? GetFirstAgent(registry);
        if (agent is null)
        {
            return Results.NotFound(new { error = "no_agent_for_skill", skill = task.Skill });
        }
        using var taskCts = CancellationTokenSource.CreateLinkedTokenSource(ctx.RequestAborted);
        taskCts.CancelAfter(options.MaxTaskDuration);
        try
        {
            var response = await agent.HandleTaskAsync(task, taskCts.Token).ConfigureAwait(false);
            return Results.Json(response);
        }
        catch (OperationCanceledException) when (taskCts.IsCancellationRequested && !ctx.RequestAborted.IsCancellationRequested)
        {
            return Results.StatusCode(StatusCodes.Status504GatewayTimeout);
        }
    }

    private static async Task TaskSendSubscribeHandler(HttpContext ctx, A2ATask task)
    {
        if (!IsAuthenticated(ctx, out var authError))
        {
            await authError!.ExecuteAsync(ctx).ConfigureAwait(false);
            return;
        }
        var registry = ctx.RequestServices.GetRequiredService<IA2AAgentRegistry>();
        var options = ctx.RequestServices.GetRequiredService<IOptions<A2AServerOptions>>().Value;
        var logger = ctx.RequestServices.GetService<ILoggerFactory>()?.CreateLogger("A2AServer.SSE");

        if (string.IsNullOrEmpty(task.Skill))
        {
            ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
            await ctx.Response.WriteAsJsonAsync(new { error = "skill_required" }).ConfigureAwait(false);
            return;
        }
        var authorization = await AuthorizeRequestAsync(ctx, task, cancellationToken: ctx.RequestAborted).ConfigureAwait(false);
        if (!authorization.IsAllowed)
        {
            ctx.Response.StatusCode = authorization.ErrorResult is not null
                ? GetStatusCode(authorization.ErrorResult)
                : StatusCodes.Status403Forbidden;
            if (authorization.ErrorResult is not null)
            {
                await authorization.ErrorResult.ExecuteAsync(ctx).ConfigureAwait(false);
            }
            return;
        }
        var agent = registry.FindByCapability(task.Skill).FirstOrDefault()
                    ?? GetFirstAgent(registry);
        if (agent is null)
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            await ctx.Response.WriteAsJsonAsync(new { error = "no_agent_for_skill", skill = task.Skill }).ConfigureAwait(false);
            return;
        }

        ctx.Response.StatusCode = StatusCodes.Status200OK;
        ctx.Response.ContentType = "text/event-stream";
        ctx.Response.Headers.CacheControl = "no-cache";
        ctx.Response.Headers["X-Accel-Buffering"] = "no";

        using var taskCts = CancellationTokenSource.CreateLinkedTokenSource(ctx.RequestAborted);
        taskCts.CancelAfter(options.MaxTaskDuration);

        try
        {
            await foreach (var evt in agent.StreamTaskAsync(task, taskCts.Token).ConfigureAwait(false))
            {
                await WriteSseEventAsync(ctx, evt, taskCts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (taskCts.IsCancellationRequested)
        {
            logger?.LogDebug("A2A SSE task {TaskId} cancelled or timed out.", task.Id);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "A2A SSE task {TaskId} threw during streaming.", task.Id);
            var err = new A2AEvent { TaskId = task.Id, EventType = "error", Payload = new { message = ex.Message } };
            try
            {
                await WriteSseEventAsync(ctx, err, CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // connection may already be closed
            }
        }
    }

    private static IResult TaskGetHandler(HttpContext ctx, string taskId)
    {
        if (!IsAuthenticated(ctx, out var authError)) return authError!;
        // MVP: task state lookup is delegated to an optional IA2ATaskStateStore (not in MVP).
        // Until that lands, return 501 Not Implemented so callers know to fall back to the
        // streaming flow rather than poll.
        return Results.StatusCode(StatusCodes.Status501NotImplemented);
    }

    private static IResult TaskCancelHandler(HttpContext ctx, string taskId)
    {
        if (!IsAuthenticated(ctx, out var authError)) return authError!;
        // MVP: cancellation lookup-by-id requires the optional state store; until that lands,
        // clients cancel by closing the SSE connection (handled via ctx.RequestAborted).
        return Results.StatusCode(StatusCodes.Status501NotImplemented);
    }

    private static bool IsAuthenticated(HttpContext ctx, out IResult? errorResult)
    {
        errorResult = null;
        var options = ctx.RequestServices.GetRequiredService<IOptions<A2AServerOptions>>().Value;
        var hasAllowlist = options.AllowedBearerTokens.Count > 0;

        if (!hasAllowlist && !options.RequireAuthentication)
        {
            return true;
        }

        var authHeader = ctx.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            errorResult = Results.Unauthorized();
            return false;
        }
        var token = authHeader["Bearer ".Length..].Trim();
        if (hasAllowlist && !options.AllowedBearerTokens.Contains(token))
        {
            errorResult = Results.Unauthorized();
            return false;
        }
        if (!hasAllowlist && options.RequireAuthentication && string.IsNullOrEmpty(token))
        {
            errorResult = Results.Unauthorized();
            return false;
        }
        return true;
    }

    private static async Task<A2ARequestAuthorizationResult> AuthorizeRequestAsync(
        HttpContext ctx,
        A2ATask task,
        CancellationToken cancellationToken)
    {
        var authorizer = ctx.RequestServices.GetService<IA2ARequestAuthorizer>();
        var policy = ctx.RequestServices.GetService<IA2ASkillPolicy>();
        if (authorizer is null || policy is null)
        {
            return A2ARequestAuthorizationResult.AllowAnonymous();
        }

        var requestContext = await authorizer.AuthorizeAsync(ctx, task, cancellationToken).ConfigureAwait(false);
        if (!requestContext.IsAllowed)
        {
            return requestContext;
        }

        if (!policy.IsAllowed(requestContext, task.Skill))
        {
            return A2ARequestAuthorizationResult.Deny(
                Results.Json(
                    new { error = "skill_forbidden", skill = task.Skill, role = requestContext.AgentRole },
                    statusCode: StatusCodes.Status403Forbidden));
        }

        return requestContext;
    }

    private static async Task WriteSseEventAsync(HttpContext ctx, A2AEvent evt, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(evt);
        var line = $"event: {evt.EventType}\ndata: {json}\n\n";
        var bytes = System.Text.Encoding.UTF8.GetBytes(line);
        await ctx.Response.Body.WriteAsync(bytes, ct).ConfigureAwait(false);
        await ctx.Response.Body.FlushAsync(ct).ConfigureAwait(false);
    }

    private static IA2AAgent? GetFirstAgent(IA2AAgentRegistry registry)
    {
        var ids = registry.List();
        if (ids.Count == 0) return null;
        return registry.GetById(ids[0]);
    }

    private static AgentCard BuildServiceCard(A2AServerOptions options, IReadOnlyList<IA2AAgent> agents)
    {
        var uniqueSkills = agents
            .SelectMany(agent => agent.GetAgentCard().Skills)
            .GroupBy(skill => skill.Name, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(skill => skill.Name, StringComparer.Ordinal)
            .ToList();

        var modes = agents
            .SelectMany(agent => agent.GetAgentCard().SupportedModes)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(mode => mode, StringComparer.Ordinal)
            .ToList();

        return new AgentCard
        {
            Name = options.ServiceName,
            Description = options.ServiceDescription,
            Version = options.ServiceVersion,
            Skills = uniqueSkills,
            SupportedModes = modes
        };
    }

    private static int GetStatusCode(IResult result)
    {
        return result switch
        {
            IStatusCodeHttpResult statusCodeResult when statusCodeResult.StatusCode is int statusCode => statusCode,
            _ => StatusCodes.Status403Forbidden
        };
    }
}

internal sealed class EndpointConventionAggregate : IEndpointConventionBuilder
{
    private readonly List<IEndpointConventionBuilder> _inner = new();

    public void Add(IEndpointConventionBuilder convention)
    {
        _inner.Add(convention);
    }

    public void Add(Action<EndpointBuilder> convention)
    {
        foreach (var c in _inner) c.Add(convention);
    }
}
