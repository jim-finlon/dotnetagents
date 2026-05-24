// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Agents.ControlLoops;

/// <summary>
/// Governance seam every control-loop layer (behavior tree, workflow trigger,
/// queue trigger) calls before a high-impact action proceeds. Story feef53a3.
///
/// Implementation lives in caller code — the ControlLoops package stays free
/// of infrastructure-specific or security-specific policy. Pilot adopters
/// (InfrastructureControl, SecurityScanning) wire their own implementations
/// against CredentialsAgent / approval queues / blast-radius classifiers.
///
/// Contract: <see cref="EvaluateAsync"/> never throws — it always returns a
/// verdict so the caller can surface a clean attention item or fall through to
/// a fallback policy. Callers that get <see cref="GovernanceVerdict.Allow"/>
/// proceed; <see cref="GovernanceVerdict.Defer"/> triggers a re-check after
/// the deferral window; <see cref="GovernanceVerdict.Deny"/> stops the action
/// and the caller surfaces the verdict reason as an <see cref="AttentionItem"/>.
/// </summary>
public interface IControlLoopGovernanceHook
{
    Task<GovernanceVerdict> EvaluateAsync(GovernanceCheckRequest request, CancellationToken ct = default);
}

/// <summary>
/// Standard request shape every governance hook receives. Story feef53a3.
/// Carries enough context that a hook can decide based on the action being
/// proposed without the caller leaking implementation details.
/// </summary>
public sealed record GovernanceCheckRequest(
    string ActionKind,
    string BlastRadius,
    string ActorId,
    string ActorType,
    ControlLoopRunContext? RunContext = null,
    GovernancePosture? CurrentPosture = null,
    IReadOnlyDictionary<string, object?>? ActionPayload = null);

public sealed record GovernanceVerdict(
    GovernanceDecision Decision,
    string ReasonCode,
    string ReasonMessage,
    AttentionItem? AttentionForOperator = null,
    TimeSpan? RetryAfter = null,
    IReadOnlyList<string>? RequiredApprovers = null)
{
    public static GovernanceVerdict Allow(string reasonCode = "allow", string reasonMessage = "policy permits") =>
        new(GovernanceDecision.Allow, reasonCode, reasonMessage);

    public static GovernanceVerdict Deny(string reasonCode, string reasonMessage, AttentionItem? attention = null) =>
        new(GovernanceDecision.Deny, reasonCode, reasonMessage, attention);

    public static GovernanceVerdict Defer(string reasonCode, string reasonMessage, TimeSpan retryAfter, AttentionItem? attention = null) =>
        new(GovernanceDecision.Defer, reasonCode, reasonMessage, attention, retryAfter);

    public static GovernanceVerdict NeedsApproval(string reasonCode, string reasonMessage, IReadOnlyList<string> approvers, AttentionItem? attention = null) =>
        new(GovernanceDecision.NeedsApproval, reasonCode, reasonMessage, attention, null, approvers);
}

public enum GovernanceDecision
{
    /// <summary>Action proceeds. The caller does the work.</summary>
    Allow,
    /// <summary>Action is rejected outright. Caller stops + surfaces attention.</summary>
    Deny,
    /// <summary>Action is delayed. Caller waits <see cref="GovernanceVerdict.RetryAfter"/> then re-evaluates.</summary>
    Defer,
    /// <summary>Action requires explicit approval from <see cref="GovernanceVerdict.RequiredApprovers"/>. Caller routes to the approval queue.</summary>
    NeedsApproval,
}

/// <summary>
/// Default permissive hook — allows every action. Story feef53a3. Useful as
/// the fallback when a service hasn't wired its real governance yet, and as
/// the test default when the system under test isn't exercising governance.
/// Production callers should replace this with a real implementation.
/// </summary>
public sealed class AlwaysAllowGovernanceHook : IControlLoopGovernanceHook
{
    public Task<GovernanceVerdict> EvaluateAsync(GovernanceCheckRequest request, CancellationToken ct = default) =>
        Task.FromResult(GovernanceVerdict.Allow("default.allow", "AlwaysAllowGovernanceHook is in effect; configure a real governance hook for production."));
}

/// <summary>
/// Composite hook that evaluates a chain of inner hooks in order. Story feef53a3.
/// Stops on the first non-Allow verdict. Use this when you have multiple
/// orthogonal governance concerns (blast radius + actor capability + deploy
/// window) — each is a separate hook and the composite enforces all of them.
/// </summary>
public sealed class CompositeGovernanceHook : IControlLoopGovernanceHook
{
    private readonly IReadOnlyList<IControlLoopGovernanceHook> _hooks;

    public CompositeGovernanceHook(params IControlLoopGovernanceHook[] hooks)
    {
        if (hooks is null || hooks.Length == 0)
            throw new ArgumentException("CompositeGovernanceHook requires at least one hook.", nameof(hooks));
        _hooks = hooks;
    }

    public async Task<GovernanceVerdict> EvaluateAsync(GovernanceCheckRequest request, CancellationToken ct = default)
    {
        foreach (var hook in _hooks)
        {
            var verdict = await hook.EvaluateAsync(request, ct).ConfigureAwait(false);
            if (verdict.Decision != GovernanceDecision.Allow) return verdict;
        }
        return GovernanceVerdict.Allow("composite.allow", "all hooks allowed the action");
    }
}
