using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Workflow.HumanInLoop;

/// <summary>
/// In-memory implementation of <see cref="IInputHandler{TState}"/> for testing and development.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public class InMemoryInputHandler<TState> : IInputHandler<TState> where TState : class
{
    private readonly ConcurrentDictionary<string, InputRequest<TState>> _pendingInputs = new();
    private readonly ConcurrentDictionary<string, object> _inputs = new();
    private readonly ILogger<InMemoryInputHandler<TState>>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryInputHandler{TState}"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for input operations.</param>
    public InMemoryInputHandler(ILogger<InMemoryInputHandler<TState>>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<TValue?> RequestInputAsync<TValue>(
        string workflowRunId,
        string nodeName,
        TState state,
        string propertyName,
        InputType inputType,
        string prompt,
        TValue? defaultValue = default,
        string? validationRule = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeName);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        cancellationToken.ThrowIfCancellationRequested();

        var key = GetInputKey(workflowRunId, nodeName, propertyName);
        var request = new InputRequest<TState>
        {
            WorkflowRunId = workflowRunId,
            NodeName = nodeName,
            State = state,
            PropertyName = propertyName,
            InputType = inputType,
            Prompt = prompt,
            DefaultValue = defaultValue,
            ValidationRule = validationRule,
            RequestedAt = DateTimeOffset.UtcNow
        };

        _pendingInputs[key] = request;
        _logger?.LogInformation(
            "Input requested for workflow '{WorkflowRunId}' at node '{NodeName}', property '{PropertyName}'. Type: {InputType}, Prompt: {Prompt}",
            workflowRunId,
            nodeName,
            propertyName,
            inputType,
            prompt);

        // Check if input already exists
        if (_inputs.TryGetValue(key, out var existingInput) && existingInput is TValue existingValue)
        {
            return Task.FromResult<TValue?>(existingValue);
        }

        // Return default if provided, otherwise null
        return Task.FromResult(defaultValue);
    }

    /// <inheritdoc/>
    public Task<TValue?> GetInputAsync<TValue>(
        string workflowRunId,
        string nodeName,
        string propertyName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        cancellationToken.ThrowIfCancellationRequested();

        var key = GetInputKey(workflowRunId, nodeName, propertyName);
        if (_inputs.TryGetValue(key, out var input) && input is TValue value)
        {
            return Task.FromResult<TValue?>(value);
        }

        return Task.FromResult<TValue?>(default);
    }

    /// <summary>
    /// Manually sets input for a pending input request.
    /// </summary>
    /// <typeparam name="TValue">The type of the input value.</typeparam>
    /// <param name="workflowRunId">The ID of the workflow run.</param>
    /// <param name="nodeName">The name of the node requesting the input.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="value">The input value.</param>
    public void SetInput<TValue>(string workflowRunId, string nodeName, string propertyName, TValue value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        var key = GetInputKey(workflowRunId, nodeName, propertyName);
        _inputs[key] = value!;
        _pendingInputs.TryRemove(key, out _);

        _logger?.LogInformation(
            "Input set for workflow '{WorkflowRunId}' at node '{NodeName}', property '{PropertyName}'. Value: {Value}",
            workflowRunId,
            nodeName,
            propertyName,
            value);
    }

    /// <summary>
    /// Gets a pending input request.
    /// </summary>
    /// <param name="workflowRunId">The ID of the workflow run.</param>
    /// <param name="nodeName">The name of the node requesting the input.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>The input request if found; otherwise, null.</returns>
    public InputRequest<TState>? GetPendingInput(string workflowRunId, string nodeName, string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        var key = GetInputKey(workflowRunId, nodeName, propertyName);
        _pendingInputs.TryGetValue(key, out var request);
        return request;
    }

    /// <summary>
    /// Gets all pending input requests for a workflow run.
    /// </summary>
    /// <param name="workflowRunId">The ID of the workflow run.</param>
    /// <returns>A list of pending input requests.</returns>
    public IReadOnlyList<InputRequest<TState>> GetPendingInputs(string workflowRunId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowRunId);

        return _pendingInputs.Values
            .Where(r => r.WorkflowRunId == workflowRunId)
            .ToList();
    }

    private static string GetInputKey(string workflowRunId, string nodeName, string propertyName)
    {
        return $"{workflowRunId}:{nodeName}:{propertyName}";
    }

    /// <summary>
    /// Represents an input request.
    /// </summary>
    /// <typeparam name="TState">The type of the workflow state.</typeparam>
    public class InputRequest<TState> where TState : class
    {
        /// <summary>
        /// Gets the workflow run ID.
        /// </summary>
        public string WorkflowRunId { get; init; } = string.Empty;

        /// <summary>
        /// Gets the node name requesting the input.
        /// </summary>
        public string NodeName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the workflow state at the time of the request.
        /// </summary>
        public TState State { get; init; } = null!;

        /// <summary>
        /// Gets the name of the property to set.
        /// </summary>
        public string PropertyName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the type of input requested.
        /// </summary>
        public InputType InputType { get; init; }

        /// <summary>
        /// Gets the prompt/question to present to the human.
        /// </summary>
        public string Prompt { get; init; } = string.Empty;

        /// <summary>
        /// Gets the optional default value.
        /// </summary>
        public object? DefaultValue { get; init; }

        /// <summary>
        /// Gets the optional validation rule.
        /// </summary>
        public string? ValidationRule { get; init; }

        /// <summary>
        /// Gets the timestamp when the input was requested.
        /// </summary>
        public DateTimeOffset RequestedAt { get; init; }
    }
}
