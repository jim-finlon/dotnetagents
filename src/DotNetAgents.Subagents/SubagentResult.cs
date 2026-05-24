// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Subagents;

/// <summary>
/// The typed outcome of a subagent execution. Carries the structured result, cost
/// attribution back to the parent task, plus the subagent's own evidence trace
/// (tool calls + final output snippet) for the parent's evidence ledger.
/// </summary>
/// <typeparam name="T">The structured result type the subagent produced.</typeparam>
/// <param name="SubagentName">Which subagent produced this result (matches the descriptor).</param>
/// <param name="Result">The structured result. Null when <paramref name="IsSuccess"/> is false.</param>
/// <param name="IsSuccess">Whether the subagent ran to completion without exception or timeout.</param>
/// <param name="ErrorMessage">Error message when <paramref name="IsSuccess"/> is false.</param>
/// <param name="TokensConsumed">Tokens charged to the parent's task; 0 when the runner doesn't track tokens.</param>
/// <param name="ToolCallTrace">Names of tools the subagent invoked (in call order). Used for evidence.</param>
/// <param name="DurationMs">Wall-clock execution time.</param>
/// <param name="StartedAtUtc">When execution started.</param>
public sealed record SubagentResult<T>(
    string SubagentName,
    T? Result,
    bool IsSuccess,
    string? ErrorMessage,
    int TokensConsumed,
    IReadOnlyList<string> ToolCallTrace,
    long DurationMs,
    DateTimeOffset StartedAtUtc)
{
    public static SubagentResult<T> Success(
        string subagentName,
        T result,
        int tokensConsumed,
        IReadOnlyList<string> toolCallTrace,
        long durationMs,
        DateTimeOffset startedAtUtc) =>
        new(subagentName, result, true, null, tokensConsumed, toolCallTrace, durationMs, startedAtUtc);

    public static SubagentResult<T> Failure(
        string subagentName,
        string errorMessage,
        int tokensConsumed,
        IReadOnlyList<string> toolCallTrace,
        long durationMs,
        DateTimeOffset startedAtUtc) =>
        new(subagentName, default, false, errorMessage, tokensConsumed, toolCallTrace, durationMs, startedAtUtc);
}
