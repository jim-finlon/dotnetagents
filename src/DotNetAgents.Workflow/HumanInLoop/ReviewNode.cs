using DotNetAgents.Abstractions.Exceptions;
using DotNetAgents.Workflow.Graph;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Workflow.HumanInLoop;

/// <summary>
/// A workflow node that requires human review (and optionally modification) of the workflow state before proceeding.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public class ReviewNode<TState> : GraphNode<TState> where TState : class
{
    private readonly IReviewHandler<TState> _reviewHandler;
    private readonly string? _context;
    private readonly bool _allowModification;
    private readonly TimeSpan? _timeout;
    private readonly ILogger<ReviewNode<TState>>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReviewNode{TState}"/> class.
    /// </summary>
    /// <param name="name">The name of the review node.</param>
    /// <param name="reviewHandler">The review handler to use.</param>
    /// <param name="context">Optional context describing what needs to be reviewed.</param>
    /// <param name="allowModification">Whether the human is allowed to modify the state. Default is true.</param>
    /// <param name="timeout">Optional timeout for the review. If null, waits indefinitely.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    public ReviewNode(
        string name,
        IReviewHandler<TState> reviewHandler,
        string? context = null,
        bool allowModification = true,
        TimeSpan? timeout = null,
        ILogger<ReviewNode<TState>>? logger = null)
        : base(name, CreateHandler(
            reviewHandler ?? throw new ArgumentNullException(nameof(reviewHandler)),
            context,
            allowModification,
            timeout,
            logger,
            name))
    {
        _reviewHandler = reviewHandler;
        _context = context;
        _allowModification = allowModification;
        _timeout = timeout;
        _logger = logger;
        Description = $"Requires human review{(context != null ? $": {context}" : "")}";
    }

    private static Func<TState, CancellationToken, Task<TState>> CreateHandler(
        IReviewHandler<TState> reviewHandler,
        string? context,
        bool allowModification,
        TimeSpan? timeout,
        ILogger<ReviewNode<TState>>? logger,
        string nodeName)
    {
        return async (state, ct) =>
        {
            ArgumentNullException.ThrowIfNull(state);
            ct.ThrowIfCancellationRequested();

            var workflowRunId = GetWorkflowRunId(state) ?? Guid.NewGuid().ToString("N");

            logger?.LogInformation(
                "Node {NodeName}: Requesting review for workflow '{WorkflowRunId}'. Context: {Context}, AllowModification: {AllowModification}",
                nodeName,
                workflowRunId,
                context ?? "No context provided",
                allowModification);

            // Request review
            var reviewedState = await reviewHandler.RequestReviewAsync(
                workflowRunId,
                nodeName,
                state,
                context,
                allowModification,
                ct).ConfigureAwait(false);

            if (reviewedState == null)
            {
                // Wait for review if not immediately available
                if (timeout.HasValue)
                {
                    using var timeoutCts = new CancellationTokenSource(timeout.Value);
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

                    var startTime = DateTimeOffset.UtcNow;
                    while (DateTimeOffset.UtcNow - startTime < timeout.Value)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(100), linkedCts.Token).ConfigureAwait(false);
                        reviewedState = await reviewHandler.GetReviewedStateAsync(workflowRunId, nodeName, linkedCts.Token).ConfigureAwait(false);
                        if (reviewedState != null)
                        {
                            break;
                        }
                    }

                    if (reviewedState == null)
                    {
                        throw new AgentException(
                            $"Review timeout after {timeout.Value.TotalSeconds} seconds for node '{nodeName}'.",
                            ErrorCategory.WorkflowError);
                    }
                }
                else
                {
                    // Poll for review
                    while (reviewedState == null)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(500), ct).ConfigureAwait(false);
                        reviewedState = await reviewHandler.GetReviewedStateAsync(workflowRunId, nodeName, ct).ConfigureAwait(false);
                    }
                }
            }

            logger?.LogInformation(
                "Node {NodeName}: Review completed for workflow '{WorkflowRunId}'. State modified: {Modified}",
                nodeName,
                workflowRunId,
                !ReferenceEquals(state, reviewedState));

            return reviewedState;
        };
    }

    private static string? GetWorkflowRunId(TState state)
    {
        var type = typeof(TState);
        var prop = type.GetProperty("WorkflowRunId") ?? type.GetProperty("RunId");
        return prop?.GetValue(state)?.ToString();
    }
}
