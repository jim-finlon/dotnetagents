namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// Represents the result of executing a behavior tree node.
/// </summary>
/// <typeparam name="TContext">The type of the execution context.</typeparam>
public record BehaviorTreeNodeResult<TContext> where TContext : class
{
    /// <summary>
    /// Gets the execution status.
    /// </summary>
    public BehaviorTreeNodeStatus Status { get; init; }

    /// <summary>
    /// Gets the context after execution.
    /// </summary>
    public TContext Context { get; init; } = null!;

    /// <summary>
    /// Gets an optional message describing the result.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets optional additional data.
    /// </summary>
    public Dictionary<string, object> Data { get; init; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BehaviorTreeNodeResult{TContext}"/> class.
    /// </summary>
    /// <param name="status">The execution status.</param>
    /// <param name="context">The context after execution.</param>
    /// <param name="message">Optional message.</param>
    public BehaviorTreeNodeResult(BehaviorTreeNodeStatus status, TContext context, string? message = null)
    {
        Status = status;
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Message = message;
    }
}
