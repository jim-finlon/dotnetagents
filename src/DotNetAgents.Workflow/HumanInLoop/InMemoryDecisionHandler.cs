using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Workflow.HumanInLoop;

/// <summary>
/// In-memory implementation of <see cref="IDecisionHandler{TState}"/> for testing and development.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public class InMemoryDecisionHandler<TState> : IDecisionHandler<TState> where TState : class
{
    private readonly ConcurrentDictionary<string, DecisionRequest<TState>> _pendingDecisions = new();
    private readonly ConcurrentDictionary<string, string> _decisions = new();
    private readonly ILogger<InMemoryDecisionHandler<TState>>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryDecisionHandler{TState}"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for decision operations.</param>
    public InMemoryDecisionHandler(ILogger<InMemoryDecisionHandler<TState>>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<string?> RequestDecisionAsync(
        string workflowRunId,
        string nodeName,
        TState state,
        string question,
        IReadOnlyList<string> options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeName);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(question);
        ArgumentNullException.ThrowIfNull(options);

        if (options.Count == 0)
        {
            throw new ArgumentException("Options list cannot be empty.", nameof(options));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var key = GetDecisionKey(workflowRunId, nodeName);
        var request = new DecisionRequest<TState>
        {
            WorkflowRunId = workflowRunId,
            NodeName = nodeName,
            State = state,
            Question = question,
            Options = options.ToList(),
            RequestedAt = DateTimeOffset.UtcNow
        };

        _pendingDecisions[key] = request;
        _logger?.LogInformation(
            "Decision requested for workflow '{WorkflowRunId}' at node '{NodeName}'. Question: {Question}, Options: {Options}",
            workflowRunId,
            nodeName,
            question,
            string.Join(", ", options));

        // Check if decision already exists
        if (_decisions.TryGetValue(key, out var existingDecision))
        {
            return Task.FromResult<string?>(existingDecision);
        }

        // Return null to indicate decision is pending
        return Task.FromResult<string?>(null);
    }

    /// <inheritdoc/>
    public Task<string?> GetDecisionAsync(
        string workflowRunId,
        string nodeName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeName);

        cancellationToken.ThrowIfCancellationRequested();

        var key = GetDecisionKey(workflowRunId, nodeName);
        if (_decisions.TryGetValue(key, out var decision))
        {
            return Task.FromResult<string?>(decision);
        }

        return Task.FromResult<string?>(null);
    }

    /// <summary>
    /// Manually sets a decision for a pending decision request.
    /// </summary>
    /// <param name="workflowRunId">The ID of the workflow run.</param>
    /// <param name="nodeName">The name of the node requesting the decision.</param>
    /// <param name="decision">The selected option.</param>
    /// <exception cref="ArgumentException">Thrown when the decision is not one of the valid options.</exception>
    public void SetDecision(string workflowRunId, string nodeName, string decision)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(decision);

        var key = GetDecisionKey(workflowRunId, nodeName);

        // Validate decision is one of the options
        if (_pendingDecisions.TryGetValue(key, out var request))
        {
            if (!request.Options.Contains(decision))
            {
                throw new ArgumentException(
                    $"Decision '{decision}' is not one of the valid options: {string.Join(", ", request.Options)}",
                    nameof(decision));
            }
        }

        _decisions[key] = decision;
        _pendingDecisions.TryRemove(key, out _);

        _logger?.LogInformation(
            "Decision set for workflow '{WorkflowRunId}' at node '{NodeName}'. Selected: {Decision}",
            workflowRunId,
            nodeName,
            decision);
    }

    /// <summary>
    /// Gets a pending decision request.
    /// </summary>
    /// <param name="workflowRunId">The ID of the workflow run.</param>
    /// <param name="nodeName">The name of the node requesting the decision.</param>
    /// <returns>The decision request if found; otherwise, null.</returns>
    public DecisionRequest<TState>? GetPendingDecision(string workflowRunId, string nodeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeName);

        var key = GetDecisionKey(workflowRunId, nodeName);
        _pendingDecisions.TryGetValue(key, out var request);
        return request;
    }

    /// <summary>
    /// Gets all pending decision requests for a workflow run.
    /// </summary>
    /// <param name="workflowRunId">The ID of the workflow run.</param>
    /// <returns>A list of pending decision requests.</returns>
    public IReadOnlyList<DecisionRequest<TState>> GetPendingDecisions(string workflowRunId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowRunId);

        return _pendingDecisions.Values
            .Where(r => r.WorkflowRunId == workflowRunId)
            .ToList();
    }

    private static string GetDecisionKey(string workflowRunId, string nodeName)
    {
        return $"{workflowRunId}:{nodeName}";
    }

    /// <summary>
    /// Represents a decision request.
    /// </summary>
    /// <typeparam name="TState">The type of the workflow state.</typeparam>
    public class DecisionRequest<TState> where TState : class
    {
        /// <summary>
        /// Gets the workflow run ID.
        /// </summary>
        public string WorkflowRunId { get; init; } = string.Empty;

        /// <summary>
        /// Gets the node name requesting the decision.
        /// </summary>
        public string NodeName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the workflow state at the time of the request.
        /// </summary>
        public TState State { get; init; } = null!;

        /// <summary>
        /// Gets the question to present to the human.
        /// </summary>
        public string Question { get; init; } = string.Empty;

        /// <summary>
        /// Gets the available options to choose from.
        /// </summary>
        public List<string> Options { get; init; } = new();

        /// <summary>
        /// Gets the timestamp when the decision was requested.
        /// </summary>
        public DateTimeOffset RequestedAt { get; init; }
    }
}
