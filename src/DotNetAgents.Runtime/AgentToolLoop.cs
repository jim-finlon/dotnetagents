using System.Text.Json;
using DotNetAgents.Abstractions.Tools;

namespace DotNetAgents.Runtime;

/// <summary>
/// Canonical single-pass tool execution for runtime sessions (story 13775408).
/// <see cref="AgentExecutor"/> performs multi-iteration ReAct; this type documents and implements
/// the shared <see cref="ITool"/> contract for one batch of planned calls.
/// </summary>
public static class AgentToolLoop
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<AgentToolLoopResult> ExecuteAsync(
        IReadOnlyList<PlannedToolCall> toolCalls,
        Func<string, CancellationToken, Task<ITool?>> resolveToolAsync,
        Func<AgentToolInvocationResult, CancellationToken, Task> recordInvocationAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolCalls);
        ArgumentNullException.ThrowIfNull(resolveToolAsync);
        ArgumentNullException.ThrowIfNull(recordInvocationAsync);

        var errorCount = 0;
        var invocations = new List<AgentToolInvocationResult>(toolCalls.Count);

        foreach (var toolCall in toolCalls)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var startedAt = DateTimeOffset.UtcNow;
            var inputJson = NormalizeInput(toolCall.Input);
            var tool = await resolveToolAsync(toolCall.ToolName, cancellationToken).ConfigureAwait(false);

            if (tool is null)
            {
                errorCount++;
                var missing = new AgentToolInvocationResult(
                    toolCall.ToolName,
                    toolCall.ToolCallId,
                    inputJson,
                    OutputJson: null,
                    Succeeded: false,
                    startedAt,
                    DateTimeOffset.UtcNow,
                    $"Tool '{toolCall.ToolName}' was not found.");
                invocations.Add(missing);
                await recordInvocationAsync(missing, cancellationToken).ConfigureAwait(false);
                continue;
            }

            try
            {
                var result = await tool.ExecuteAsync(toolCall.Input, cancellationToken).ConfigureAwait(false);
                if (!result.IsSuccess)
                    errorCount++;

                var invocation = new AgentToolInvocationResult(
                    tool.Name,
                    toolCall.ToolCallId,
                    inputJson,
                    SerializeSafe(result.Output),
                    result.IsSuccess,
                    startedAt,
                    DateTimeOffset.UtcNow,
                    result.ErrorMessage);
                invocations.Add(invocation);
                await recordInvocationAsync(invocation, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                errorCount++;
                var failed = new AgentToolInvocationResult(
                    tool.Name,
                    toolCall.ToolCallId,
                    inputJson,
                    OutputJson: null,
                    Succeeded: false,
                    startedAt,
                    DateTimeOffset.UtcNow,
                    ex.Message);
                invocations.Add(failed);
                await recordInvocationAsync(failed, cancellationToken).ConfigureAwait(false);
            }
        }

        return new AgentToolLoopResult(errorCount, invocations);
    }

    public static string NormalizeInput(object? input) => SerializeSafe(input);

    internal static string SerializeSafe(object? value)
    {
        if (value is null)
            return "{}";

        if (value is string s)
            return s;

        return JsonSerializer.Serialize(value, JsonOptions);
    }
}

/// <summary>One tool call outcome from <see cref="AgentToolLoop"/>.</summary>
public sealed record AgentToolInvocationResult(
    string ToolName,
    string? ToolCallId,
    string InputJson,
    string? OutputJson,
    bool Succeeded,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    string? ErrorMessage);

/// <summary>Aggregate result for a batch of tool calls.</summary>
public sealed record AgentToolLoopResult(int ErrorCount, IReadOnlyList<AgentToolInvocationResult> Invocations);
