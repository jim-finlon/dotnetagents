// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Core.Agents;
using DotNetAgents.Abstractions.Models;
using DotNetAgents.Abstractions.Prompts;
using DotNetAgents.Abstractions.Tools;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// A behavior tree node that uses an LLM to decide what action to take.
/// </summary>
/// <typeparam name="TContext">The type of the execution context.</typeparam>
public class LLMActionNode<TContext> : BehaviorTreeNode<TContext> where TContext : class
{
    private readonly AgentExecutor _agentExecutor;
    private readonly Func<TContext, string> _contextToPrompt;
    private readonly Func<string, TContext, TContext> _resultToContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="LLMActionNode{TContext}"/> class.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="agentExecutor">The agent executor to use for LLM-based decisions.</param>
    /// <param name="contextToPrompt">Function to convert context to a prompt string.</param>
    /// <param name="resultToContext">Function to update context with LLM result.</param>
    /// <param name="logger">Optional logger instance.</param>
    public LLMActionNode(
        string name,
        AgentExecutor agentExecutor,
        Func<TContext, string> contextToPrompt,
        Func<string, TContext, TContext> resultToContext,
        ILogger<BehaviorTreeNode<TContext>>? logger = null)
        : base(name, logger)
    {
        _agentExecutor = agentExecutor ?? throw new ArgumentNullException(nameof(agentExecutor));
        _contextToPrompt = contextToPrompt ?? throw new ArgumentNullException(nameof(contextToPrompt));
        _resultToContext = resultToContext ?? throw new ArgumentNullException(nameof(resultToContext));
    }

    /// <inheritdoc/>
    public override async Task<BehaviorTreeNodeStatus> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        Logger?.LogDebug("Executing LLM action node '{NodeName}'", Name);

        try
        {
            var prompt = _contextToPrompt(context);
            var result = await _agentExecutor.InvokeAsync(prompt, cancellationToken: cancellationToken).ConfigureAwait(false);

            // Update context with result
            var updatedContext = _resultToContext(result, context);

            Logger?.LogDebug("LLM action node '{NodeName}' completed successfully", Name);
            return BehaviorTreeNodeStatus.Success;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "LLM action node '{NodeName}' failed", Name);
            return BehaviorTreeNodeStatus.Failure;
        }
    }
}
