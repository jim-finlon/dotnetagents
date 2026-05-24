// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Workflow.HumanInLoop;

/// <summary>
/// In-memory implementation of <see cref="IApprovalHandler{TState}"/> for testing and development.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public class InMemoryApprovalHandler<TState> : IApprovalHandler<TState> where TState : class
{
    private readonly ConcurrentDictionary<string, ApprovalRequest<TState>> _pendingApprovals = new();
    private readonly ConcurrentDictionary<string, bool> _approvals = new();
    private readonly ILogger<InMemoryApprovalHandler<TState>>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryApprovalHandler{TState}"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for approval operations.</param>
    public InMemoryApprovalHandler(ILogger<InMemoryApprovalHandler<TState>>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<bool> RequestApprovalAsync(
        string workflowRunId,
        string nodeName,
        TState state,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeName);
        ArgumentNullException.ThrowIfNull(state);

        cancellationToken.ThrowIfCancellationRequested();

        var key = GetApprovalKey(workflowRunId, nodeName);
        var request = new ApprovalRequest<TState>
        {
            WorkflowRunId = workflowRunId,
            NodeName = nodeName,
            State = state,
            Message = message,
            RequestedAt = DateTimeOffset.UtcNow
        };

        _pendingApprovals[key] = request;
        _logger?.LogInformation(
            "Approval requested for workflow '{WorkflowRunId}' at node '{NodeName}'. Message: {Message}",
            workflowRunId,
            nodeName,
            message ?? "No message provided");

        // In a real implementation, this would wait for human approval
        // For now, we return false to indicate approval is pending
        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public Task<bool> IsApprovedAsync(
        string workflowRunId,
        string nodeName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeName);

        cancellationToken.ThrowIfCancellationRequested();

        var key = GetApprovalKey(workflowRunId, nodeName);
        if (_approvals.TryGetValue(key, out var approved))
        {
            return Task.FromResult(approved);
        }

        return Task.FromResult(false);
    }

    /// <summary>
    /// Manually approves a pending approval request.
    /// </summary>
    /// <param name="workflowRunId">The ID of the workflow run.</param>
    /// <param name="nodeName">The name of the node requesting approval.</param>
    /// <param name="approved">True to approve; false to reject.</param>
    public void Approve(string workflowRunId, string nodeName, bool approved = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeName);

        var key = GetApprovalKey(workflowRunId, nodeName);
        _approvals[key] = approved;
        _pendingApprovals.TryRemove(key, out _);

        _logger?.LogInformation(
            "Approval {Status} for workflow '{WorkflowRunId}' at node '{NodeName}'.",
            approved ? "granted" : "rejected",
            workflowRunId,
            nodeName);
    }

    /// <summary>
    /// Gets a pending approval request.
    /// </summary>
    /// <param name="workflowRunId">The ID of the workflow run.</param>
    /// <param name="nodeName">The name of the node requesting approval.</param>
    /// <returns>The approval request if found; otherwise, null.</returns>
    public ApprovalRequest<TState>? GetPendingApproval(string workflowRunId, string nodeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeName);

        var key = GetApprovalKey(workflowRunId, nodeName);
        _pendingApprovals.TryGetValue(key, out var request);
        return request;
    }

    /// <summary>
    /// Gets all pending approval requests for a workflow run.
    /// </summary>
    /// <param name="workflowRunId">The ID of the workflow run.</param>
    /// <returns>A list of pending approval requests.</returns>
    public IReadOnlyList<ApprovalRequest<TState>> GetPendingApprovals(string workflowRunId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowRunId);

        return _pendingApprovals.Values
            .Where(r => r.WorkflowRunId == workflowRunId)
            .ToList();
    }

    private static string GetApprovalKey(string workflowRunId, string nodeName)
    {
        return $"{workflowRunId}:{nodeName}";
    }

    /// <summary>
    /// Represents an approval request.
    /// </summary>
    /// <typeparam name="TApprovalState">The type of the workflow state.</typeparam>
    public class ApprovalRequest<TApprovalState> where TApprovalState : class
    {
        /// <summary>
        /// Gets the workflow run ID.
        /// </summary>
        public string WorkflowRunId { get; init; } = string.Empty;

        /// <summary>
        /// Gets the node name requesting approval.
        /// </summary>
        public string NodeName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the workflow state at the time of the request.
        /// </summary>
        public TApprovalState State { get; init; } = null!;

        /// <summary>
        /// Gets the optional message describing what needs approval.
        /// </summary>
        public string? Message { get; init; }

        /// <summary>
        /// Gets the timestamp when the approval was requested.
        /// </summary>
        public DateTimeOffset RequestedAt { get; init; }
    }
}
