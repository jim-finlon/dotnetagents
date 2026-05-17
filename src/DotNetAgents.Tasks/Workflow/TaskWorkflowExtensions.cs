using DotNetAgents.Tasks.Models;
using DotNetAgents.Workflow.Graph;

namespace DotNetAgents.Tasks.Workflow;

/// <summary>
/// Extension methods for integrating tasks with workflows.
/// </summary>
public static class TaskWorkflowExtensions
{
    /// <summary>
    /// Creates a task node that creates a new task when executed.
    /// </summary>
    /// <typeparam name="TState">The type of the workflow state.</typeparam>
    /// <param name="graph">The state graph.</param>
    /// <param name="nodeName">The name of the node.</param>
    /// <param name="taskFactory">Factory function to create the task from the state.</param>
    /// <param name="taskManager">The task manager instance.</param>
    /// <returns>The graph for chaining.</returns>
    public static StateGraph<TState> AddTaskCreationNode<TState>(
        this StateGraph<TState> graph,
        string nodeName,
        Func<TState, ITaskManager, CancellationToken, Task<WorkTask>> taskFactory,
        ITaskManager taskManager)
        where TState : class
    {
        if (graph == null)
            throw new ArgumentNullException(nameof(graph));

        if (string.IsNullOrWhiteSpace(nodeName))
            throw new ArgumentException("Node name cannot be null or whitespace.", nameof(nodeName));

        if (taskFactory == null)
            throw new ArgumentNullException(nameof(taskFactory));

        if (taskManager == null)
            throw new ArgumentNullException(nameof(taskManager));

        graph.AddNode(nodeName, async (state, cancellationToken) =>
        {
            var task = await taskFactory(state, taskManager, cancellationToken).ConfigureAwait(false);
            return state;
        });

        return graph;
    }

    /// <summary>
    /// Creates a task update node that updates an existing task when executed.
    /// </summary>
    /// <typeparam name="TState">The type of the workflow state.</typeparam>
    /// <param name="graph">The state graph.</param>
    /// <param name="nodeName">The name of the node.</param>
    /// <param name="taskUpdater">Function to update the task from the state.</param>
    /// <param name="taskManager">The task manager instance.</param>
    /// <returns>The graph for chaining.</returns>
    public static StateGraph<TState> AddTaskUpdateNode<TState>(
        this StateGraph<TState> graph,
        string nodeName,
        Func<TState, ITaskManager, CancellationToken, Task<WorkTask>> taskUpdater,
        ITaskManager taskManager)
        where TState : class
    {
        if (graph == null)
            throw new ArgumentNullException(nameof(graph));

        if (string.IsNullOrWhiteSpace(nodeName))
            throw new ArgumentException("Node name cannot be null or whitespace.", nameof(nodeName));

        if (taskUpdater == null)
            throw new ArgumentNullException(nameof(taskUpdater));

        if (taskManager == null)
            throw new ArgumentNullException(nameof(taskManager));

        graph.AddNode(nodeName, async (state, cancellationToken) =>
        {
            var task = await taskUpdater(state, taskManager, cancellationToken).ConfigureAwait(false);
            return state;
        });

        return graph;
    }
}
