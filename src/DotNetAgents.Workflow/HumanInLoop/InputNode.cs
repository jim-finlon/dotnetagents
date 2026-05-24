// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.Exceptions;
using DotNetAgents.Workflow.Graph;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace DotNetAgents.Workflow.HumanInLoop;

/// <summary>
/// A workflow node that requires human input before proceeding.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public class InputNode<TState> : GraphNode<TState> where TState : class
{
    private readonly IInputHandler<TState> _inputHandler;
    private readonly string _propertyName;
    private readonly InputType _inputType;
    private readonly string _prompt;
    private readonly object? _defaultValue;
    private readonly string? _validationRule;
    private readonly TimeSpan? _timeout;
    private readonly ILogger<InputNode<TState>>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputNode{TState}"/> class.
    /// </summary>
    /// <param name="name">The name of the input node.</param>
    /// <param name="inputHandler">The input handler to use.</param>
    /// <param name="propertyName">The name of the property to set with the input value.</param>
    /// <param name="inputType">The type of input requested.</param>
    /// <param name="prompt">The prompt/question to present to the human.</param>
    /// <param name="defaultValue">Optional default value.</param>
    /// <param name="validationRule">Optional validation rule.</param>
    /// <param name="timeout">Optional timeout for the input. If null, waits indefinitely.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    public InputNode(
        string name,
        IInputHandler<TState> inputHandler,
        string propertyName,
        InputType inputType,
        string prompt,
        object? defaultValue = null,
        string? validationRule = null,
        TimeSpan? timeout = null,
        ILogger<InputNode<TState>>? logger = null)
        : base(name, CreateHandler(
            inputHandler ?? throw new ArgumentNullException(nameof(inputHandler)),
            propertyName ?? throw new ArgumentNullException(nameof(propertyName)),
            inputType,
            prompt ?? throw new ArgumentNullException(nameof(prompt)),
            defaultValue,
            validationRule,
            timeout,
            logger,
            name))
    {
        _inputHandler = inputHandler;
        _propertyName = propertyName;
        _inputType = inputType;
        _prompt = prompt;
        _defaultValue = defaultValue;
        _validationRule = validationRule;
        _timeout = timeout;
        _logger = logger;
        Description = $"Requires human input: {prompt}";
    }

    private static Func<TState, CancellationToken, Task<TState>> CreateHandler(
        IInputHandler<TState> inputHandler,
        string propertyName,
        InputType inputType,
        string prompt,
        object? defaultValue,
        string? validationRule,
        TimeSpan? timeout,
        ILogger<InputNode<TState>>? logger,
        string nodeName)
    {
        return async (state, ct) =>
        {
            ArgumentNullException.ThrowIfNull(state);
            ct.ThrowIfCancellationRequested();

            var workflowRunId = GetWorkflowRunId(state) ?? Guid.NewGuid().ToString("N");

            logger?.LogInformation(
                "Node {NodeName}: Requesting input for workflow '{WorkflowRunId}'. Property: {PropertyName}, Type: {InputType}, Prompt: {Prompt}",
                nodeName,
                workflowRunId,
                propertyName,
                inputType,
                prompt);

            // Get the property type
            var property = typeof(TState).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
            {
                throw new AgentException(
                    $"Property '{propertyName}' not found on type '{typeof(TState).Name}'.",
                    ErrorCategory.WorkflowError);
            }

            // Request input based on property type
            // Use reflection to call the generic method with the correct type
            object? inputValue = null;
            var propertyType = property.PropertyType;
            var nullablePropertyType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            var isNullable = propertyType != nullablePropertyType;

            // Get the generic method
            var method = typeof(IInputHandler<TState>).GetMethod(nameof(IInputHandler<TState>.RequestInputAsync))!;
            var genericMethod = method.MakeGenericMethod(nullablePropertyType);

            // Prepare parameters
            var parameters = new object?[]
            {
                workflowRunId,
                nodeName,
                state,
                propertyName,
                inputType,
                prompt,
                defaultValue, // Will be converted by the handler
                validationRule,
                ct
            };

            // Call the method
            var task = (Task)genericMethod.Invoke(inputHandler, parameters)!;
            await task.ConfigureAwait(false);

            // Get the result using reflection
            var resultProperty = task.GetType().GetProperty("Result");
            inputValue = resultProperty?.GetValue(task);

            if (inputValue == null)
            {
                // Wait for input if not immediately available
                if (timeout.HasValue)
                {
                    using var timeoutCts = new CancellationTokenSource(timeout.Value);
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

                    var startTime = DateTimeOffset.UtcNow;
                    while (DateTimeOffset.UtcNow - startTime < timeout.Value)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(100), linkedCts.Token).ConfigureAwait(false);

                        // Get input using reflection
                        var getMethod = typeof(IInputHandler<TState>).GetMethod(nameof(IInputHandler<TState>.GetInputAsync))!;
                        var getGenericMethod = getMethod.MakeGenericMethod(nullablePropertyType);
                        var getTask = (Task)getGenericMethod.Invoke(inputHandler, new object?[] { workflowRunId, nodeName, propertyName, linkedCts.Token })!;
                        await getTask.ConfigureAwait(false);

                        var getResultProperty = getTask.GetType().GetProperty("Result");
                        inputValue = getResultProperty?.GetValue(getTask);

                        if (inputValue != null)
                        {
                            break;
                        }
                    }

                    if (inputValue == null)
                    {
                        throw new AgentException(
                            $"Input timeout after {timeout.Value.TotalSeconds} seconds for node '{nodeName}', property '{propertyName}'.",
                            ErrorCategory.WorkflowError);
                    }
                }
                else
                {
                    // Poll for input using reflection
                    var getMethod = typeof(IInputHandler<TState>).GetMethod(nameof(IInputHandler<TState>.GetInputAsync))!;
                    var getGenericMethod = getMethod.MakeGenericMethod(nullablePropertyType);

                    while (inputValue == null)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(500), ct).ConfigureAwait(false);

                        var getTask = (Task)getGenericMethod.Invoke(inputHandler, new object?[] { workflowRunId, nodeName, propertyName, ct })!;
                        await getTask.ConfigureAwait(false);

                        var getResultProperty = getTask.GetType().GetProperty("Result");
                        inputValue = getResultProperty?.GetValue(getTask);
                    }
                }
            }

            // Set the property value
            if (inputValue != null && property.CanWrite)
            {
                try
                {
                    // Convert to the property type if needed
                    var convertedValue = Convert.ChangeType(inputValue, nullablePropertyType);
                    property.SetValue(state, convertedValue);
                }
                catch (Exception ex)
                {
                    throw new AgentException(
                        $"Failed to set property '{propertyName}' with input value: {ex.Message}",
                        ErrorCategory.WorkflowError);
                }
            }

            logger?.LogInformation(
                "Node {NodeName}: Input received for workflow '{WorkflowRunId}'. Property: {PropertyName}, Value: {Value}",
                nodeName,
                workflowRunId,
                propertyName,
                inputValue);

            return state;
        };
    }

    private static string? GetWorkflowRunId(TState state)
    {
        var type = typeof(TState);
        var prop = type.GetProperty("WorkflowRunId") ?? type.GetProperty("RunId");
        return prop?.GetValue(state)?.ToString();
    }
}
