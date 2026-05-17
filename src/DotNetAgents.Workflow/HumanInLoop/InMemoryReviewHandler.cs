using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Workflow.HumanInLoop;

/// <summary>
/// In-memory implementation of <see cref="IReviewHandler{TState}"/> for testing and development.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public class InMemoryReviewHandler<TState> : IReviewHandler<TState> where TState : class
{
    private readonly ConcurrentDictionary<string, ReviewRequest<TState>> _pendingReviews = new();
    private readonly ConcurrentDictionary<string, TState> _reviewedStates = new();
    private readonly ILogger<InMemoryReviewHandler<TState>>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryReviewHandler{TState}"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for review operations.</param>
    public InMemoryReviewHandler(ILogger<InMemoryReviewHandler<TState>>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<TState?> RequestReviewAsync(
        string workflowRunId,
        string nodeName,
        TState state,
        string? context = null,
        bool allowModification = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeName);
        ArgumentNullException.ThrowIfNull(state);

        cancellationToken.ThrowIfCancellationRequested();

        var key = GetReviewKey(workflowRunId, nodeName);
        var request = new ReviewRequest<TState>
        {
            WorkflowRunId = workflowRunId,
            NodeName = nodeName,
            State = state,
            Context = context,
            AllowModification = allowModification,
            RequestedAt = DateTimeOffset.UtcNow
        };

        _pendingReviews[key] = request;
        _logger?.LogInformation(
            "Review requested for workflow '{WorkflowRunId}' at node '{NodeName}'. Context: {Context}, AllowModification: {AllowModification}",
            workflowRunId,
            nodeName,
            context ?? "No context provided",
            allowModification);

        // Check if review already exists
        if (_reviewedStates.TryGetValue(key, out var existingState))
        {
            return Task.FromResult<TState?>(existingState);
        }

        // Return null to indicate review is pending
        return Task.FromResult<TState?>(null);
    }

    /// <inheritdoc/>
    public Task<TState?> GetReviewedStateAsync(
        string workflowRunId,
        string nodeName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeName);

        cancellationToken.ThrowIfCancellationRequested();

        var key = GetReviewKey(workflowRunId, nodeName);
        if (_reviewedStates.TryGetValue(key, out var reviewedState))
        {
            return Task.FromResult<TState?>(reviewedState);
        }

        return Task.FromResult<TState?>(null);
    }

    /// <summary>
    /// Manually sets the reviewed state for a pending review request.
    /// </summary>
    /// <param name="workflowRunId">The ID of the workflow run.</param>
    /// <param name="nodeName">The name of the node requesting the review.</param>
    /// <param name="reviewedState">The reviewed (and potentially modified) state.</param>
    public void SetReviewedState(string workflowRunId, string nodeName, TState reviewedState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeName);
        ArgumentNullException.ThrowIfNull(reviewedState);

        var key = GetReviewKey(workflowRunId, nodeName);
        _reviewedStates[key] = reviewedState;
        _pendingReviews.TryRemove(key, out _);

        _logger?.LogInformation(
            "Review completed for workflow '{WorkflowRunId}' at node '{NodeName}'.",
            workflowRunId,
            nodeName);
    }

    /// <summary>
    /// Gets a pending review request.
    /// </summary>
    /// <param name="workflowRunId">The ID of the workflow run.</param>
    /// <param name="nodeName">The name of the node requesting the review.</param>
    /// <returns>The review request if found; otherwise, null.</returns>
    public ReviewRequest<TState>? GetPendingReview(string workflowRunId, string nodeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeName);

        var key = GetReviewKey(workflowRunId, nodeName);
        _pendingReviews.TryGetValue(key, out var request);
        return request;
    }

    /// <summary>
    /// Gets all pending review requests for a workflow run.
    /// </summary>
    /// <param name="workflowRunId">The ID of the workflow run.</param>
    /// <returns>A list of pending review requests.</returns>
    public IReadOnlyList<ReviewRequest<TState>> GetPendingReviews(string workflowRunId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowRunId);

        return _pendingReviews.Values
            .Where(r => r.WorkflowRunId == workflowRunId)
            .ToList();
    }

    private static string GetReviewKey(string workflowRunId, string nodeName)
    {
        return $"{workflowRunId}:{nodeName}";
    }

    /// <summary>
    /// Represents a review request.
    /// </summary>
    /// <typeparam name="TState">The type of the workflow state.</typeparam>
    public class ReviewRequest<TState> where TState : class
    {
        /// <summary>
        /// Gets the workflow run ID.
        /// </summary>
        public string WorkflowRunId { get; init; } = string.Empty;

        /// <summary>
        /// Gets the node name requesting the review.
        /// </summary>
        public string NodeName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the workflow state to review.
        /// </summary>
        public TState State { get; init; } = null!;

        /// <summary>
        /// Gets the optional context describing what needs to be reviewed.
        /// </summary>
        public string? Context { get; init; }

        /// <summary>
        /// Gets whether the human is allowed to modify the state.
        /// </summary>
        public bool AllowModification { get; init; }

        /// <summary>
        /// Gets the timestamp when the review was requested.
        /// </summary>
        public DateTimeOffset RequestedAt { get; init; }
    }
}
