// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.Exceptions;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Workflow.Graph;

/// <summary>
/// A workflow node that executes a child node repeatedly until a condition is met.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public class LoopNode<TState> : GraphNode<TState> where TState : class
{
    private readonly GraphNode<TState> _childNode;
    private readonly Func<TState, bool> _continueCondition;
    private readonly Func<TState, bool>? _breakCondition;
    private readonly int? _maxIterations;
    private readonly ILogger<LoopNode<TState>>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoopNode{TState}"/> class.
    /// </summary>
    /// <param name="name">The name of the loop node.</param>
    /// <param name="childNode">The child node to execute in the loop.</param>
    /// <param name="continueCondition">A function that returns true to continue looping, false to exit.</param>
    /// <param name="breakCondition">Optional function that returns true to break out of the loop immediately.</param>
    /// <param name="maxIterations">Optional maximum number of iterations. If null, loops indefinitely until condition is met.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    public LoopNode(
        string name,
        GraphNode<TState> childNode,
        Func<TState, bool> continueCondition,
        Func<TState, bool>? breakCondition = null,
        int? maxIterations = null,
        ILogger<LoopNode<TState>>? logger = null)
        : base(name, CreateHandler(
            childNode ?? throw new ArgumentNullException(nameof(childNode)),
            continueCondition ?? throw new ArgumentNullException(nameof(continueCondition)),
            breakCondition,
            maxIterations,
            logger,
            name))
    {
        _childNode = childNode;
        _continueCondition = continueCondition;
        _breakCondition = breakCondition;
        _maxIterations = maxIterations;
        _logger = logger;
        Description = $"Loops executing {childNode.Name} until condition is met" +
                     (maxIterations.HasValue ? $" (max {maxIterations} iterations)" : "");
    }

    private static Func<TState, CancellationToken, Task<TState>> CreateHandler(
        GraphNode<TState> childNode,
        Func<TState, bool> continueCondition,
        Func<TState, bool>? breakCondition,
        int? maxIterations,
        ILogger<LoopNode<TState>>? logger,
        string nodeName)
    {
        return async (state, ct) =>
        {
            ArgumentNullException.ThrowIfNull(state);
            ct.ThrowIfCancellationRequested();

            var iteration = 0;
            var currentState = state;

            logger?.LogInformation(
                "Node {NodeName}: Starting loop execution. Max iterations: {MaxIterations}",
                nodeName,
                maxIterations?.ToString() ?? "unlimited");

            while (true)
            {
                iteration++;
                ct.ThrowIfCancellationRequested();

                // Check max iterations
                if (maxIterations.HasValue && iteration > maxIterations.Value)
                {
                    logger?.LogWarning(
                        "Node {NodeName}: Maximum iterations ({MaxIterations}) reached. Exiting loop.",
                        nodeName,
                        maxIterations.Value);
                    break;
                }

                // Check break condition
                if (breakCondition != null)
                {
                    try
                    {
                        if (breakCondition(currentState))
                        {
                            logger?.LogInformation(
                                "Node {NodeName}: Break condition met at iteration {Iteration}. Exiting loop.",
                                nodeName,
                                iteration);
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Error evaluating break condition at iteration {Iteration}.", iteration);
                        // Continue execution - don't break on condition evaluation errors
                    }
                }

                // Check continue condition
                bool shouldContinue;
                try
                {
                    shouldContinue = continueCondition(currentState);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error evaluating continue condition at iteration {Iteration}.", iteration);
                    throw new AgentException(
                        $"Continue condition evaluation failed at iteration {iteration}: {ex.Message}",
                        ErrorCategory.WorkflowError,
                        ex);
                }

                if (!shouldContinue)
                {
                    logger?.LogInformation(
                        "Node {NodeName}: Continue condition returned false at iteration {Iteration}. Exiting loop.",
                        nodeName,
                        iteration);
                    break;
                }

                logger?.LogDebug(
                    "Node {NodeName}: Executing iteration {Iteration}",
                    nodeName,
                    iteration);

                // Execute child node
                try
                {
                    currentState = await childNode.ExecuteAsync(currentState, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger?.LogError(
                        ex,
                        "Node {NodeName}: Error executing child node at iteration {Iteration}",
                        nodeName,
                        iteration);
                    throw new AgentException(
                        $"Loop execution failed at iteration {iteration}: {ex.Message}",
                        ErrorCategory.WorkflowError,
                        ex);
                }
            }

            logger?.LogInformation(
                "Node {NodeName}: Loop completed after {Iterations} iterations.",
                nodeName,
                iteration);

            return currentState;
        };
    }
}
