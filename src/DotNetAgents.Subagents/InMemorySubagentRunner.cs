using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Subagents;

/// <summary>
/// Reference <see cref="ISubagentRunner"/> implementation that runs subagents via an injected
/// execution delegate. The delegate encapsulates the actual LLM/tool dispatch — production
/// deployments wire it to JARVIS's MCP dispatch with the subagent's allowedTools as the
/// effective tool set; tests inject a deterministic delegate.
/// </summary>
/// <remarks>
/// Context isolation is enforced structurally: the runner builds a fresh execution context
/// per subagent and never reuses parent state. Cost attribution to the parent task is wired
/// via the <see cref="SubagentExecutionContext.ParentTaskId"/> field that the delegate sees.
/// Depth tracking is performed by the runner; the delegate may pass a child runner instance
/// configured with depth-1 if it needs to spawn nested subagents.
/// </remarks>
public sealed class InMemorySubagentRunner : ISubagentRunner
{
    /// <summary>
    /// Delegate invoked to actually run the subagent. Implementations dispatch to LLM + tools;
    /// returns the structured result + token usage + tool-call trace.
    /// </summary>
    public delegate Task<SubagentExecutionResult> ExecuteDelegate(
        SubagentExecutionContext context,
        CancellationToken cancellationToken);

    private readonly ExecuteDelegate _executeAsync;
    private readonly ILogger<InMemorySubagentRunner>? _logger;
    private readonly int _currentDepth;
    private readonly int _maxParallelism;

    public InMemorySubagentRunner(
        ExecuteDelegate executeAsync,
        ILogger<InMemorySubagentRunner>? logger = null,
        int maxParallelism = 10,
        int currentDepth = 0)
    {
        _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        _logger = logger;
        _currentDepth = currentDepth;
        _maxParallelism = maxParallelism > 0 ? maxParallelism : 10;
    }

    /// <inheritdoc />
    public async Task<SubagentResult<TResult>> RunAsync<TResult>(
        SubagentDescriptor descriptor,
        string taskInput,
        string parentTaskId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(taskInput);
        ArgumentNullException.ThrowIfNull(parentTaskId);

        if (_currentDepth >= descriptor.MaxDepth)
        {
            return SubagentResult<TResult>.Failure(
                descriptor.Name,
                $"Subagent recursion depth limit ({descriptor.MaxDepth}) exceeded; runner already at depth {_currentDepth}.",
                tokensConsumed: 0,
                toolCallTrace: Array.Empty<string>(),
                durationMs: 0,
                startedAtUtc: DateTimeOffset.UtcNow);
        }

        var sw = Stopwatch.StartNew();
        var startedAt = DateTimeOffset.UtcNow;

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(descriptor.TimeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var execCtx = new SubagentExecutionContext(
            Descriptor: descriptor,
            TaskInput: taskInput,
            ParentTaskId: parentTaskId,
            CurrentDepth: _currentDepth);

        try
        {
            var execResult = await _executeAsync(execCtx, linkedCts.Token).ConfigureAwait(false);

            sw.Stop();
            return SubagentResult<TResult>.Success(
                descriptor.Name,
                (TResult)execResult.Result!,
                execResult.TokensConsumed,
                execResult.ToolCallTrace,
                sw.ElapsedMilliseconds,
                startedAt);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            sw.Stop();
            return SubagentResult<TResult>.Failure(
                descriptor.Name,
                $"Subagent timed out after {descriptor.TimeoutSeconds}s.",
                tokensConsumed: 0,
                toolCallTrace: Array.Empty<string>(),
                durationMs: sw.ElapsedMilliseconds,
                startedAtUtc: startedAt);
        }
        catch (OperationCanceledException)
        {
            // Caller-initiated cancellation — propagate.
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger?.LogWarning(ex, "Subagent {Name} threw during execution.", descriptor.Name);
            return SubagentResult<TResult>.Failure(
                descriptor.Name,
                $"{ex.GetType().Name}: {ex.Message}",
                tokensConsumed: 0,
                toolCallTrace: Array.Empty<string>(),
                durationMs: sw.ElapsedMilliseconds,
                startedAtUtc: startedAt);
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<SubagentResult<TResult>> RunAllAsync<TResult>(
        IReadOnlyList<(SubagentDescriptor Descriptor, string TaskInput)> tasks,
        string parentTaskId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        ArgumentNullException.ThrowIfNull(parentTaskId);

        if (tasks.Count == 0)
        {
            yield break;
        }

        // Cap parallelism at _maxParallelism — process in batches.
        for (var batchStart = 0; batchStart < tasks.Count; batchStart += _maxParallelism)
        {
            var batch = tasks
                .Skip(batchStart)
                .Take(_maxParallelism)
                .Select(t => RunAsync<TResult>(t.Descriptor, t.TaskInput, parentTaskId, cancellationToken))
                .ToArray();

            // Stream results as each completes (interleaved within batch).
            while (batch.Length > 0)
            {
                var completed = await Task.WhenAny(batch).ConfigureAwait(false);
                yield return await completed.ConfigureAwait(false);
                batch = batch.Where(t => t != completed).ToArray();
            }
        }
    }
}

/// <summary>Context passed to the execution delegate. Read-only.</summary>
public sealed record SubagentExecutionContext(
    SubagentDescriptor Descriptor,
    string TaskInput,
    string ParentTaskId,
    int CurrentDepth);

/// <summary>Result returned by the execution delegate.</summary>
public sealed record SubagentExecutionResult(
    object Result,
    int TokensConsumed,
    IReadOnlyList<string> ToolCallTrace);
