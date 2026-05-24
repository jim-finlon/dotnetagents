// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.Hooks;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Core.Hooks;

/// <summary>
/// Executes a hook chain at a lifecycle checkpoint. Hooks subscribed to the checkpoint run
/// in priority-ascending order; the first non-Allow decision short-circuits the chain. Hook
/// exceptions are caught and treated as Allow (so misbehaving hooks never silently halt the
/// agent loop) — the exception is logged and surfaced via the optional <see cref="HookExceptionEventArgs"/>
/// event so operators can audit hook health.
/// </summary>
public sealed class HookChainExecutor
{
    private readonly ILogger<HookChainExecutor>? _logger;

    /// <summary>Raised when a hook throws during evaluation. Subscribers can record evidence or alert operators.</summary>
    public event EventHandler<HookExceptionEventArgs>? HookException;

    public HookChainExecutor(ILogger<HookChainExecutor>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Run the hook chain for the given checkpoint and return the first non-Allow decision
    /// encountered (short-circuit) or Allow if the chain completes without intervention.
    /// </summary>
    public async Task<HookChainResult> EvaluateAsync(
        AgentHookContext context,
        IEnumerable<IAgentHook> hooks,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(hooks);

        var subscribed = hooks
            .Where(h => h.SubscribedCheckpoints.Contains(context.Checkpoint))
            .OrderBy(h => h.Priority)
            .ThenBy(h => h.Id, StringComparer.Ordinal)
            .ToArray();

        if (subscribed.Length == 0)
        {
            return new HookChainResult(HookDecision.Allow, EvaluatedHooks: Array.Empty<HookEvaluation>());
        }

        var evaluations = new List<HookEvaluation>(subscribed.Length);

        foreach (var hook in subscribed)
        {
            cancellationToken.ThrowIfCancellationRequested();

            HookDecision decision;
            var startedAt = DateTimeOffset.UtcNow;
            try
            {
                decision = await hook.EvaluateAsync(context, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex,
                    "Hook {HookId} threw during {Checkpoint} for actor {ActorId}; treated as Allow.",
                    hook.Id, context.Checkpoint, context.ActorId);
                HookException?.Invoke(this, new HookExceptionEventArgs(hook.Id, context.Checkpoint, ex));
                evaluations.Add(new HookEvaluation(
                    hook.Id, HookDecision.Allow, startedAt, DateTimeOffset.UtcNow, Exception: ex));
                continue;
            }

            evaluations.Add(new HookEvaluation(hook.Id, decision, startedAt, DateTimeOffset.UtcNow));

            if (decision is not HookDecision.AllowDecision)
            {
                return new HookChainResult(decision, evaluations);
            }
        }

        return new HookChainResult(HookDecision.Allow, evaluations);
    }
}

/// <summary>
/// The outcome of running a hook chain at a checkpoint. Carries the final decision plus the
/// per-hook evaluations so callers can attach the chain trace to evidence.
/// </summary>
/// <param name="FinalDecision">The decision the agent loop should honor.</param>
/// <param name="EvaluatedHooks">The chain trace — one entry per hook actually evaluated (in order, ending at the short-circuiting hook if any).</param>
public sealed record HookChainResult(
    HookDecision FinalDecision,
    IReadOnlyList<HookEvaluation> EvaluatedHooks);

/// <summary>One evaluation in a hook chain trace.</summary>
public sealed record HookEvaluation(
    string HookId,
    HookDecision Decision,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EndedAtUtc,
    Exception? Exception = null);

/// <summary>Raised when a hook throws. Carries the failing hook id, checkpoint, and exception.</summary>
public sealed class HookExceptionEventArgs : EventArgs
{
    public string HookId { get; }
    public HookCheckpoint Checkpoint { get; }
    public Exception Exception { get; }

    public HookExceptionEventArgs(string hookId, HookCheckpoint checkpoint, Exception exception)
    {
        HookId = hookId;
        Checkpoint = checkpoint;
        Exception = exception;
    }
}
