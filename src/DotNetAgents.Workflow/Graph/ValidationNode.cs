using DotNetAgents.Abstractions.Exceptions;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Workflow.Graph;

/// <summary>
/// A workflow node that validates the workflow state before proceeding.
/// Can optionally branch to different nodes based on validation result.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public class ValidationNode<TState> : GraphNode<TState> where TState : class
{
    private readonly Func<TState, CancellationToken, Task<ValidationResult>> _validator;
    private readonly string _validationResultPropertyName;
    private readonly bool _throwOnFailure;
    private readonly ILogger<ValidationNode<TState>>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationNode{TState}"/> class.
    /// </summary>
    /// <param name="name">The name of the validation node.</param>
    /// <param name="validator">A function that validates the state and returns a ValidationResult.</param>
    /// <param name="validationResultPropertyName">The name of the property to store the validation result. Default is "ValidationResult".</param>
    /// <param name="throwOnFailure">Whether to throw an exception on validation failure. Default is true.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    public ValidationNode(
        string name,
        Func<TState, CancellationToken, Task<ValidationResult>> validator,
        string validationResultPropertyName = "ValidationResult",
        bool throwOnFailure = true,
        ILogger<ValidationNode<TState>>? logger = null)
        : base(name, CreateHandler(
            validator ?? throw new ArgumentNullException(nameof(validator)),
            validationResultPropertyName ?? throw new ArgumentNullException(nameof(validationResultPropertyName)),
            throwOnFailure,
            logger,
            name))
    {
        _validator = validator;
        _validationResultPropertyName = validationResultPropertyName;
        _throwOnFailure = throwOnFailure;
        _logger = logger;
        Description = "Validates workflow state before proceeding";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationNode{TState}"/> class with a synchronous validator.
    /// </summary>
    /// <param name="name">The name of the validation node.</param>
    /// <param name="validator">A function that validates the state and returns a ValidationResult.</param>
    /// <param name="validationResultPropertyName">The name of the property to store the validation result. Default is "ValidationResult".</param>
    /// <param name="throwOnFailure">Whether to throw an exception on validation failure. Default is true.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    public ValidationNode(
        string name,
        Func<TState, ValidationResult> validator,
        string validationResultPropertyName = "ValidationResult",
        bool throwOnFailure = true,
        ILogger<ValidationNode<TState>>? logger = null)
        : this(
            name,
            (state, ct) => Task.FromResult(validator(state)),
            validationResultPropertyName,
            throwOnFailure,
            logger)
    {
    }

    private static Func<TState, CancellationToken, Task<TState>> CreateHandler(
        Func<TState, CancellationToken, Task<ValidationResult>> validator,
        string validationResultPropertyName,
        bool throwOnFailure,
        ILogger<ValidationNode<TState>>? logger,
        string nodeName)
    {
        return async (state, ct) =>
        {
            ArgumentNullException.ThrowIfNull(state);
            ct.ThrowIfCancellationRequested();

            logger?.LogDebug("Node {NodeName}: Starting validation.", nodeName);

            ValidationResult result;
            try
            {
                result = await validator(state, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Node {NodeName}: Error during validation.", nodeName);
                throw new AgentException(
                    $"Validation failed with exception in node '{nodeName}': {ex.Message}",
                    ErrorCategory.WorkflowError,
                    ex);
            }

            // Store validation result in state
            SetValidationResultInState(state, validationResultPropertyName, result);

            if (!result.IsValid)
            {
                var errorMessage = result.ErrorMessage ?? string.Join("; ", result.Errors);
                logger?.LogWarning(
                    "Node {NodeName}: Validation failed. Errors: {Errors}",
                    nodeName,
                    errorMessage);

                if (throwOnFailure)
                {
                    throw new AgentException(
                        $"Validation failed in node '{nodeName}': {errorMessage}",
                        ErrorCategory.WorkflowError);
                }
            }
            else
            {
                logger?.LogInformation("Node {NodeName}: Validation passed.", nodeName);
            }

            return state;
        };
    }

    private static void SetValidationResultInState(TState state, string propertyName, ValidationResult result)
    {
        var type = typeof(TState);
        var prop = type.GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (prop != null && prop.CanWrite)
        {
            try
            {
                // Try to set as ValidationResult
                if (prop.PropertyType == typeof(ValidationResult))
                {
                    prop.SetValue(state, result);
                }
                // Try to set as bool
                else if (prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?))
                {
                    prop.SetValue(state, result.IsValid);
                }
                // Try to set as string (error message)
                else if (prop.PropertyType == typeof(string))
                {
                    prop.SetValue(state, result.IsValid ? null : result.ErrorMessage);
                }
            }
            catch
            {
                // Ignore if we can't set the property
            }
        }
    }

    /// <summary>
    /// Creates a conditional edge function that checks if validation passed.
    /// This can be used with WorkflowBuilder.AddEdge to create conditional edges based on validation result.
    /// </summary>
    /// <param name="validationResultPropertyName">The property name that stores the validation result. Default is "ValidationResult".</param>
    /// <returns>A condition function that returns true if validation passed.</returns>
    public static Func<TState, bool> CreateValidationPassedCondition(string validationResultPropertyName = "ValidationResult")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(validationResultPropertyName);

        return (state) =>
        {
            if (state == null)
                return false;

            var type = typeof(TState);
            var prop = type.GetProperty(validationResultPropertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (prop == null)
                return false;

            try
            {
                var value = prop.GetValue(state);
                if (value is ValidationResult result)
                {
                    return result.IsValid;
                }
                if (value is bool boolValue)
                {
                    return boolValue;
                }
                return value == null; // null string means valid
            }
            catch
            {
                return false;
            }
        };
    }
}
