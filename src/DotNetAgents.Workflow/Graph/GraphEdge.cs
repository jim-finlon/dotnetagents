// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Workflow.Graph;

/// <summary>
/// Represents an edge (transition) between nodes in a workflow graph.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public class GraphEdge<TState> where TState : class
{
    /// <summary>
    /// Gets the name of the source node.
    /// </summary>
    public string From { get; }

    /// <summary>
    /// Gets the name of the target node.
    /// </summary>
    public string To { get; }

    /// <summary>
    /// Gets an optional condition function that determines if this edge should be taken.
    /// If null, the edge is unconditional (always taken).
    /// </summary>
    public Func<TState, bool>? Condition { get; }

    /// <summary>
    /// Gets or sets a description of what this edge represents.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphEdge{TState}"/> class.
    /// </summary>
    /// <param name="from">The name of the source node.</param>
    /// <param name="to">The name of the target node.</param>
    /// <param name="condition">An optional condition function. If null, the edge is unconditional.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="from"/> or <paramref name="to"/> is null.</exception>
    public GraphEdge(string from, string to, Func<TState, bool>? condition = null)
    {
        From = from ?? throw new ArgumentNullException(nameof(from));
        To = to ?? throw new ArgumentNullException(nameof(to));
        Condition = condition;
    }

    /// <summary>
    /// Determines if this edge should be taken given the current state.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <returns>True if the edge should be taken; otherwise, false.</returns>
    public bool ShouldTake(TState state)
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));

        return Condition == null || Condition(state);
    }
}
