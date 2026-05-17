using DotNetAgents.Abstractions.Chains;
using DotNetAgents.Abstractions.Memory;
using DotNetAgents.Abstractions.Models;
using DotNetAgents.Abstractions.Prompts;
using DotNetAgents.Abstractions.Tools;
using DotNetAgents.Abstractions.Exceptions;
using DotNetAgents.Core.Agents.StateMachines;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DotNetAgents.Core.Agents;

/// <summary>
/// Executes an agent with tool-calling capabilities using the ReAct pattern (multi-iteration).
/// For session/trajectory-backed single turns use <c>DotNetAgents.Runtime.AgentRuntime</c> instead;
/// both honor <see cref="ITool"/> — see <c>docs/architecture/AGENT-EXECUTOR-VS-AGENT-RUNTIME.md</c>.
/// </summary>
public class AgentExecutor : IRunnable<string, string>
{
    private readonly ILLMModel<string, string> _llm;
    private readonly IToolRegistry _toolRegistry;
    private readonly IPromptTemplate _promptTemplate;
    private readonly IMemory? _memory;
    private readonly int _maxIterations;
    private readonly string _stopSequence;
    private readonly IAgentExecutionStateMachine<AgentExecutionContext>? _stateMachine;
    private readonly ILogger<AgentExecutor>? _logger;
    private readonly Dictionary<string, AgentExecutionContext> _executionContexts = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentExecutor"/> class.
    /// </summary>
    /// <param name="llm">The LLM model to use.</param>
    /// <param name="toolRegistry">The registry of available tools.</param>
    /// <param name="promptTemplate">The prompt template for the agent.</param>
    /// <param name="memory">Optional memory for conversation history.</param>
    /// <param name="maxIterations">Maximum number of iterations before stopping (default: 10).</param>
    /// <param name="stopSequence">Sequence that indicates the agent should stop (default: "Final Answer:").</param>
    /// <param name="stateMachine">Optional state machine for tracking execution lifecycle.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    public AgentExecutor(
        ILLMModel<string, string> llm,
        IToolRegistry toolRegistry,
        IPromptTemplate promptTemplate,
        IMemory? memory = null,
        int maxIterations = 10,
        string stopSequence = "Final Answer:",
        IAgentExecutionStateMachine<AgentExecutionContext>? stateMachine = null,
        ILogger<AgentExecutor>? logger = null)
    {
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _promptTemplate = promptTemplate ?? throw new ArgumentNullException(nameof(promptTemplate));
        _memory = memory;
        _stateMachine = stateMachine;
        _logger = logger;

        if (maxIterations <= 0)
            throw new ArgumentException("MaxIterations must be positive.", nameof(maxIterations));

        _maxIterations = maxIterations;
        _stopSequence = stopSequence ?? throw new ArgumentNullException(nameof(stopSequence));
    }

    /// <inheritdoc/>
    public async Task<string> InvokeAsync(
        string input,
        RunnableOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be null or whitespace.", nameof(input));

        // Initialize execution context and state machine
        AgentExecutionContext? executionContext = null;
        if (_stateMachine != null)
        {
            executionContext = new AgentExecutionContext
            {
                ExecutionId = Guid.NewGuid().ToString(),
                Input = input,
                MaxIterations = _maxIterations,
                InitializedAt = DateTimeOffset.UtcNow,
                IsActive = true
            };
            lock (_executionContexts)
            {
                _executionContexts[executionContext.ExecutionId] = executionContext;
            }

            try
            {
                await _stateMachine.TransitionAsync("Initialized", executionContext, cancellationToken).ConfigureAwait(false);
                await _stateMachine.TransitionAsync("Thinking", executionContext, cancellationToken).ConfigureAwait(false);
                executionContext.ThinkingStartedAt = DateTimeOffset.UtcNow;
                _logger?.LogDebug("Agent execution {ExecutionId} transitioned to Thinking", executionContext.ExecutionId);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to transition to Thinking state");
            }
        }

        var iteration = 0;
        var conversationHistory = new List<string>();

        // Add memory context if available
        if (_memory != null)
        {
            var recentMessages = await _memory.GetMessagesAsync(5, cancellationToken).ConfigureAwait(false);
            foreach (var message in recentMessages)
            {
                conversationHistory.Add($"{message.Role}: {message.Content}");
            }
        }

        var currentInput = input;

        try
        {
            while (iteration < _maxIterations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                iteration++;

                if (executionContext != null)
                {
                    executionContext.Iteration = iteration;
                }

                // Build prompt with tools and conversation history
                var promptVariables = BuildPromptVariables(currentInput, conversationHistory);
                var formattedPrompt = await _promptTemplate.FormatAsync(promptVariables, cancellationToken).ConfigureAwait(false);

                // Call LLM (Thinking state)
                var response = await _llm.GenerateAsync(formattedPrompt, cancellationToken: cancellationToken).ConfigureAwait(false);

                // Check for stop sequence
                if (response.Contains(_stopSequence, StringComparison.OrdinalIgnoreCase))
                {
                    var finalAnswer = ExtractFinalAnswer(response);

                    // Transition to Finalizing state
                    if (_stateMachine != null && executionContext != null)
                    {
                        try
                        {
                            executionContext.FinalizingStartedAt = DateTimeOffset.UtcNow;
                            executionContext.HasFinalAnswer = true;
                            await _stateMachine.TransitionAsync("Finalizing", executionContext, cancellationToken).ConfigureAwait(false);
                            _logger?.LogDebug("Agent execution {ExecutionId} transitioned to Finalizing", executionContext.ExecutionId);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Failed to transition to Finalizing state");
                        }
                    }

                    // Save to memory if available
                    if (_memory != null)
                    {
                        await _memory.AddMessageAsync(new DotNetAgents.Abstractions.Memory.MemoryMessage
                        {
                            Content = input,
                            Role = "user"
                        }, cancellationToken).ConfigureAwait(false);

                        await _memory.AddMessageAsync(new DotNetAgents.Abstractions.Memory.MemoryMessage
                        {
                            Content = finalAnswer,
                            Role = "assistant"
                        }, cancellationToken).ConfigureAwait(false);
                    }

                    if (executionContext != null)
                    {
                        executionContext.CompletedAt = DateTimeOffset.UtcNow;
                        executionContext.IsActive = false;
                    }

                    return finalAnswer;
                }

                // Try to parse tool call
                var toolCall = ParseToolCall(response);
                if (toolCall != null)
                {
                    // Transition to Acting state
                    if (_stateMachine != null && executionContext != null)
                    {
                        try
                        {
                            executionContext.ActingStartedAt = DateTimeOffset.UtcNow;
                            executionContext.SelectedToolName = toolCall.ToolName;
                            await _stateMachine.TransitionAsync("Acting", executionContext, cancellationToken).ConfigureAwait(false);
                            _logger?.LogDebug("Agent execution {ExecutionId} transitioned to Acting (tool: {ToolName})", executionContext.ExecutionId, toolCall.ToolName);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Failed to transition to Acting state");
                        }
                    }

                    var tool = _toolRegistry.GetTool(toolCall.ToolName);
                    if (tool == null)
                    {
                        conversationHistory.Add($"Agent: {response}");
                        conversationHistory.Add($"System: Tool '{toolCall.ToolName}' not found.");
                        currentInput = $"Tool '{toolCall.ToolName}' not found. Please try again.";

                        // Transition back to Thinking
                        if (_stateMachine != null && executionContext != null)
                        {
                            try
                            {
                                await _stateMachine.TransitionAsync("Thinking", executionContext, cancellationToken).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogWarning(ex, "Failed to transition back to Thinking state");
                            }
                        }
                        continue;
                    }

                    // Execute tool
                    ToolResult toolResult;
                    try
                    {
                        toolResult = await tool.ExecuteAsync(toolCall.Arguments, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        toolResult = ToolResult.Failure($"Tool execution failed: {ex.Message}");

                        // Transition to Error state
                        if (_stateMachine != null && executionContext != null)
                        {
                            try
                            {
                                executionContext.ErrorCount++;
                                executionContext.LastErrorMessage = ex.Message;
                                await _stateMachine.TransitionAsync("Error", executionContext, cancellationToken).ConfigureAwait(false);
                                _logger?.LogWarning("Agent execution {ExecutionId} transitioned to Error", executionContext.ExecutionId);
                            }
                            catch (Exception stateEx)
                            {
                                _logger?.LogWarning(stateEx, "Failed to transition to Error state");
                            }
                        }
                    }

                    // Transition to Observing state
                    if (_stateMachine != null && executionContext != null && toolResult.IsSuccess)
                    {
                        try
                        {
                            executionContext.ObservingStartedAt = DateTimeOffset.UtcNow;
                            executionContext.ToolsExecuted++;
                            await _stateMachine.TransitionAsync("Observing", executionContext, cancellationToken).ConfigureAwait(false);
                            _logger?.LogDebug("Agent execution {ExecutionId} transitioned to Observing", executionContext.ExecutionId);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Failed to transition to Observing state");
                        }
                    }

                    // Add to conversation history
                    conversationHistory.Add($"Agent: {response}");
                    var argsString = toolCall.Arguments is JsonElement json ? json.ToString() : toolCall.Arguments.ToString() ?? "{}";
                    conversationHistory.Add($"Tool: {toolCall.ToolName}({argsString})");
                    var resultString = toolResult.IsSuccess
                        ? (toolResult.Output?.ToString() ?? "Success")
                        : (toolResult.ErrorMessage ?? "Unknown error");
                    conversationHistory.Add($"Tool Result: {resultString}");

                    // Continue with tool result as input (transition back to Thinking)
                    var toolOutput = toolResult.IsSuccess
                        ? (toolResult.Output?.ToString() ?? "Success")
                        : (toolResult.ErrorMessage ?? "Unknown error");
                    currentInput = $"Tool '{toolCall.ToolName}' returned: {toolOutput}";

                    // Transition back to Thinking for next iteration
                    if (_stateMachine != null && executionContext != null)
                    {
                        try
                        {
                            executionContext.ThinkingStartedAt = DateTimeOffset.UtcNow;
                            await _stateMachine.TransitionAsync("Thinking", executionContext, cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Failed to transition back to Thinking state");
                        }
                    }
                }
                else
                {
                    // No tool call detected, add response to history and continue
                    conversationHistory.Add($"Agent: {response}");
                    currentInput = response;
                }
            }

            // Max iterations exceeded - transition to Error
            if (_stateMachine != null && executionContext != null)
            {
                try
                {
                    executionContext.ErrorCount++;
                    executionContext.LastErrorMessage = $"Exceeded maximum iterations ({_maxIterations})";
                    await _stateMachine.TransitionAsync("Error", executionContext, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to transition to Error state");
                }
            }

            throw new AgentException(
                $"Agent exceeded maximum iterations ({_maxIterations}).",
                ErrorCategory.WorkflowError);
        }
        catch (Exception ex) when (!(ex is AgentException))
        {
            // Transition to Error state on exception
            if (_stateMachine != null && executionContext != null)
            {
                try
                {
                    executionContext.ErrorCount++;
                    executionContext.LastErrorMessage = ex.Message;
                    await _stateMachine.TransitionAsync("Error", executionContext, cancellationToken).ConfigureAwait(false);
                    _logger?.LogWarning("Agent execution {ExecutionId} transitioned to Error due to exception", executionContext.ExecutionId);
                }
                catch (Exception stateEx)
                {
                    _logger?.LogWarning(stateEx, "Failed to transition to Error state");
                }
            }
            throw;
        }
        finally
        {
            // Clean up execution context
            if (executionContext != null)
            {
                lock (_executionContexts)
                {
                    _executionContexts.Remove(executionContext.ExecutionId);
                }
            }
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> StreamAsync(
        string input,
        RunnableOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // For streaming, we'll just yield the final result
        var result = await InvokeAsync(input, options, cancellationToken).ConfigureAwait(false);
        yield return result;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> BatchAsync(
        IEnumerable<string> inputs,
        RunnableOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (inputs == null)
            throw new ArgumentNullException(nameof(inputs));

        var results = new List<string>();
        foreach (var input in inputs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await InvokeAsync(input, options, cancellationToken).ConfigureAwait(false);
            results.Add(result);
        }

        return results;
    }

    private IDictionary<string, object> BuildPromptVariables(string input, List<string> conversationHistory)
    {
        var variables = new Dictionary<string, object>
        {
            ["input"] = input,
            ["tools"] = FormatToolsForPrompt(),
            ["tool_names"] = string.Join(", ", _toolRegistry.GetAllTools().Select(t => t.Name)),
            ["conversation_history"] = conversationHistory.Count > 0
                ? string.Join("\n", conversationHistory)
                : string.Empty
        };

        return variables;
    }

    private string FormatToolsForPrompt()
    {
        var toolDescriptions = _toolRegistry.GetAllTools()
            .Select(tool => $"- {tool.Name}: {tool.Description}")
            .ToList();

        return string.Join("\n", toolDescriptions);
    }

    private static string ExtractFinalAnswer(string response)
    {
        var stopIndex = response.IndexOf("Final Answer:", StringComparison.OrdinalIgnoreCase);
        if (stopIndex >= 0)
        {
            return response.Substring(stopIndex + "Final Answer:".Length).Trim();
        }

        return response;
    }

    private static ToolCall? ParseToolCall(string response)
    {
        // Simple pattern matching for tool calls
        // Format: Action: ToolName
        // Action Input: {json}
        var actionPattern = @"Action:\s*(\w+)";
        var actionInputPattern = @"Action Input:\s*(.+?)(?:\n|$)";

        var actionMatch = Regex.Match(response, actionPattern, RegexOptions.IgnoreCase);
        if (!actionMatch.Success)
        {
            return null;
        }

        var toolName = actionMatch.Groups[1].Value;
        var inputMatch = Regex.Match(response, actionInputPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        object arguments;
        if (inputMatch.Success)
        {
            var inputText = inputMatch.Groups[1].Value.Trim();
            // Try to parse as JSON
            try
            {
                arguments = JsonSerializer.Deserialize<JsonElement>(inputText);
            }
            catch
            {
                // If not valid JSON, wrap in a simple object
                arguments = JsonSerializer.Deserialize<JsonElement>($"{{\"input\": \"{inputText.Replace("\"", "\\\"", StringComparison.Ordinal)}\"}}");
            }
        }
        else
        {
            arguments = JsonSerializer.Deserialize<JsonElement>("{}");
        }

        return new ToolCall(toolName, arguments);
    }

    private sealed record ToolCall(string ToolName, object Arguments);
}
