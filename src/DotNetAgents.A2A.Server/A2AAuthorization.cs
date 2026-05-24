// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.A2A;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetAgents.A2A.Server;

/// <summary>
/// Resolved authorization context for one inbound A2A call.
/// </summary>
public sealed record A2ARequestAuthorizationResult
{
    public bool IsAllowed { get; init; }
    public string? ActorId { get; init; }
    public string? ActorType { get; init; }
    public string? AgentRole { get; init; }
    public IResult? ErrorResult { get; init; }

    public static A2ARequestAuthorizationResult AllowAnonymous() => new()
    {
        IsAllowed = true
    };

    public static A2ARequestAuthorizationResult Allow(
        string? actorId = null,
        string? actorType = null,
        string? agentRole = null) => new()
        {
            IsAllowed = true,
            ActorId = actorId,
            ActorType = actorType,
            AgentRole = agentRole
        };

    public static A2ARequestAuthorizationResult Deny(IResult errorResult) => new()
    {
        IsAllowed = false,
        ErrorResult = errorResult
    };
}

/// <summary>
/// Resolves caller identity/trust for one inbound A2A request.
/// </summary>
public interface IA2ARequestAuthorizer
{
    Task<A2ARequestAuthorizationResult> AuthorizeAsync(
        HttpContext httpContext,
        A2ATask task,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Skill-level allow/deny policy for inbound A2A calls after the caller has been resolved.
/// </summary>
public interface IA2ASkillPolicy
{
    bool IsAllowed(A2ARequestAuthorizationResult authorization, string skillName);
}

/// <summary>
/// Default no-op policy used by hosts that do not yet enforce per-role skill allowlists.
/// </summary>
public sealed class AllowAllA2ASkillPolicy : IA2ASkillPolicy
{
    public bool IsAllowed(A2ARequestAuthorizationResult authorization, string skillName) =>
        !string.IsNullOrWhiteSpace(skillName);
}

/// <summary>
/// Default authorizer: new A2A surfaces are loopback-only until a host installs a stronger
/// trust-binding adapter.
/// </summary>
public sealed class LoopbackOnlyA2ARequestAuthorizer : IA2ARequestAuthorizer
{
    public Task<A2ARequestAuthorizationResult> AuthorizeAsync(
        HttpContext httpContext,
        A2ATask task,
        CancellationToken cancellationToken = default)
    {
        var options = httpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<A2AServerOptions>>().Value;
        if (options.AllowNonLoopbackRequests)
        {
            return Task.FromResult(A2ARequestAuthorizationResult.Allow());
        }

        var remoteIp = httpContext.Connection.RemoteIpAddress;
        if (remoteIp is not null && System.Net.IPAddress.IsLoopback(remoteIp))
        {
            return Task.FromResult(A2ARequestAuthorizationResult.Allow(
                actorId: "loopback",
                actorType: "Loopback",
                agentRole: "Local"));
        }

        return Task.FromResult(A2ARequestAuthorizationResult.Deny(
            Results.Json(
                new { error = "non_loopback_a2a_denied" },
                statusCode: StatusCodes.Status403Forbidden)));
    }
}
