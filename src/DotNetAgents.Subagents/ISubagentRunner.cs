// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Subagents;

/// <summary>
/// Runs a subagent in an isolated context window and returns a structured result.
/// </summary>
/// <remarks>
/// <para>
/// Implementations MUST guarantee context isolation: the parent agent's conversation history,
/// tool registry, and memory state MUST NOT leak into the subagent's execution. The subagent
/// receives only its descriptor's instructions + the task input; it returns only the structured
/// result. This is the architectural property that makes subagents useful — parent context
/// stays clean.
/// </para>
/// <para>
/// Cost attribution: token usage MUST be reported to the parent's cost ledger via
/// <see cref="SubagentResult{T}.TokensConsumed"/>. The subagent's own LLM calls are charged
/// to the parent's task id, not to a separate ledger.
/// </para>
/// <para>
/// Depth limit: implementations MUST refuse to spawn subagents at nesting depth greater than
/// <see cref="SubagentDescriptor.MaxDepth"/>. Tracking is via the runner's internal state;
/// a subagent runner passed to a subagent is configured with depth-1.
/// </para>
/// </remarks>
public interface ISubagentRunner
{
    /// <summary>
    /// Run a single subagent and return its result.
    /// </summary>
    Task<SubagentResult<TResult>> RunAsync<TResult>(
        SubagentDescriptor descriptor,
        string taskInput,
        string parentTaskId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Run multiple subagents in parallel against per-subagent task inputs and stream results
    /// as each completes. Total fan-out is capped by the runner's configuration (default 10).
    /// </summary>
    /// <remarks>
    /// Failure of one subagent MUST NOT cancel its siblings. Cancellation of the supplied token
    /// propagates to all in-flight subagents, but the aggregate returns whatever results
    /// completed plus failure entries for the cancelled ones.
    /// </remarks>
    IAsyncEnumerable<SubagentResult<TResult>> RunAllAsync<TResult>(
        IReadOnlyList<(SubagentDescriptor Descriptor, string TaskInput)> tasks,
        string parentTaskId,
        CancellationToken cancellationToken = default);
}
