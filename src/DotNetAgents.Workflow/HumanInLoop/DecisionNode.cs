// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.Exceptions;
using DotNetAgents.Workflow.Graph;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Workflow.HumanInLoop;

/// <summary>
/// A workflow node that requires a human decision from multiple options before proceeding.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public class DecisionNode<TState> : GraphNode<TState> where TState : class
{
    private readonly IDecisionHandler<TState> _decisionHandler;
    private readonly string _question;
    private readonly IReadOnlyList<string> _options;
    private readonly TimeSpan? _timeout;
    private readonly ILogger<DecisionNode<TState>>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DecisionNode{TState}"/> class.
    /// </summary>
    /// <param name="name">The name of the decision node.</param>
    /// <param name="decisionHandler">The decision handler to use.</param>
    /// <param name="question">The question to present to the human.</param>
    /// <param name="options">The available options to choose from.</param>
    /// <param name="timeout">Optional timeout for the decision. If null, waits indefinitely.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    /// <exception cref="ArgumentException">Thrown when options list is empty.</exception>
    public DecisionNode(
        string name,
        IDecisionHandler<TState> decisionHandler,
        string question,
        IReadOnlyList<string> options,
        TimeSpan? timeout = null,
        ILogger<DecisionNode<TState>>? logger = null)
        : base(name, CreateHandler(
            decisionHandler ?? throw new ArgumentNullException(nameof(decisionHandler)),
            question ?? throw new ArgumentNullException(nameof(question)),
            options ?? throw new ArgumentNullException(nameof(options)),
            options.Count == 0 ? throw new ArgumentException("Options list cannot be empty.", nameof(options)) : options,
            timeout,
            logger,
            name))
    {
        _decisionHandler = decisionHandler;
        _question = question;
        _options = options;
        _timeout = timeout;
        _logger = logger;
        Description = $"Requires human decision: {question}";
    }

    private static Func<TState, CancellationToken, Task<TState>> CreateHandler(
        IDecisionHandler<TState> decisionHandler,
        string question,
        IReadOnlyList<string> options,
        IReadOnlyList<string> validatedOptions,
        TimeSpan? timeout,
        ILogger<DecisionNode<TState>>? logger,
        string nodeName)
    {
        return async (state, ct) =>
        {
            ArgumentNullException.ThrowIfNull(state);
            ct.ThrowIfCancellationRequested();

            var workflowRunId = GetWorkflowRunId(state) ?? Guid.NewGuid().ToString("N");

            logger?.LogInformation(
                "Node {NodeName}: Requesting decision for workflow '{WorkflowRunId}'. Question: {Question}",
                nodeName,
                workflowRunId,
                question);

            // Request decision
            var decision = await decisionHandler.RequestDecisionAsync(
                workflowRunId,
                nodeName,
                state,
                question,
                validatedOptions,
                ct).ConfigureAwait(false);

            if (decision == null)
            {
                // Wait for decision if not immediately available
                if (timeout.HasValue)
                {
                    // Use a wall-clock deadline only. Do not cancel GetDecisionAsync with a linked timeout token:
                    // the handler should keep polling until the deadline, then throw a workflow error (not OCE).
                    var deadline = DateTimeOffset.UtcNow + timeout.Value;
                    while (decision == null && DateTimeOffset.UtcNow < deadline)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(100), ct).ConfigureAwait(false);
                        decision = await decisionHandler.GetDecisionAsync(workflowRunId, nodeName, ct).ConfigureAwait(false);
                    }

                    if (decision == null)
                    {
                        throw new AgentException(
                            $"Decision timeout after {timeout.Value.TotalSeconds} seconds for node '{nodeName}'.",
                            ErrorCategory.WorkflowError);
                    }
                }
                else
                {
                    // Poll for decision
                    while (decision == null)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(500), ct).ConfigureAwait(false);
                        decision = await decisionHandler.GetDecisionAsync(workflowRunId, nodeName, ct).ConfigureAwait(false);
                    }
                }
            }

            // Validate decision is one of the options
            if (!validatedOptions.Contains(decision))
            {
                throw new AgentException(
                    $"Invalid decision '{decision}' for node '{nodeName}'. Must be one of: {string.Join(", ", validatedOptions)}",
                    ErrorCategory.WorkflowError);
            }

            logger?.LogInformation(
                "Node {NodeName}: Decision received for workflow '{WorkflowRunId}'. Selected: {Decision}",
                nodeName,
                workflowRunId,
                decision);

            // Store decision in state if state has a Decision property
            SetDecisionInState(state, decision);

            return state;
        };
    }

    private static string? GetWorkflowRunId(TState state)
    {
        var type = typeof(TState);
        var prop = type.GetProperty("WorkflowRunId") ?? type.GetProperty("RunId");
        return prop?.GetValue(state)?.ToString();
    }

    private static void SetDecisionInState(TState state, string decision)
    {
        var type = typeof(TState);
        var prop = type.GetProperty("Decision") ?? type.GetProperty("SelectedOption");
        if (prop != null && prop.CanWrite)
        {
            try
            {
                prop.SetValue(state, decision);
            }
            catch
            {
                // Ignore if we can't set the property
            }
        }
    }
}
