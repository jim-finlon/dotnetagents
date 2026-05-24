// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Workflow.Graph;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Workflow.Templates;

/// <summary>
/// Represents a parameterized workflow template that can be instantiated with different parameters.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public class WorkflowTemplate<TState> where TState : class
{
    private readonly Func<WorkflowTemplateParameters, StateGraph<TState>> _workflowFactory;
    private readonly ILogger<WorkflowTemplate<TState>>? _logger;

    /// <summary>
    /// Gets the template name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the template description.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the template version.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets the required parameters for this template.
    /// </summary>
    public IReadOnlyDictionary<string, TemplateParameter> RequiredParameters { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowTemplate{TState}"/> class.
    /// </summary>
    /// <param name="name">The template name.</param>
    /// <param name="workflowFactory">A function that creates a workflow graph from template parameters.</param>
    /// <param name="requiredParameters">The required parameters for this template.</param>
    /// <param name="description">Optional template description.</param>
    /// <param name="version">Template version. Default is "1.0.0".</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    public WorkflowTemplate(
        string name,
        Func<WorkflowTemplateParameters, StateGraph<TState>> workflowFactory,
        IReadOnlyDictionary<string, TemplateParameter> requiredParameters,
        string? description = null,
        string version = "1.0.0",
        ILogger<WorkflowTemplate<TState>>? logger = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _workflowFactory = workflowFactory ?? throw new ArgumentNullException(nameof(workflowFactory));
        RequiredParameters = requiredParameters ?? throw new ArgumentNullException(nameof(requiredParameters));
        Description = description;
        Version = version ?? throw new ArgumentNullException(nameof(version));
        _logger = logger;
    }

    /// <summary>
    /// Creates a workflow instance from this template with the provided parameters.
    /// </summary>
    /// <param name="parameters">The parameters to use for instantiation.</param>
    /// <returns>A configured workflow graph.</returns>
    /// <exception cref="ArgumentException">Thrown when required parameters are missing or invalid.</exception>
    public StateGraph<TState> CreateWorkflow(WorkflowTemplateParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        _logger?.LogInformation(
            "Creating workflow from template '{TemplateName}' version {Version}",
            Name,
            Version);

        // Validate required parameters
        ValidateParameters(parameters);

        try
        {
            var workflow = _workflowFactory(parameters);

            if (workflow == null)
            {
                throw new InvalidOperationException(
                    $"Workflow factory returned null for template '{Name}'.");
            }

            _logger?.LogDebug(
                "Successfully created workflow from template '{TemplateName}'. Nodes: {NodeCount}",
                Name,
                workflow.Nodes.Count);

            return workflow;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating workflow from template '{TemplateName}'.", Name);
            throw new InvalidOperationException(
                $"Failed to create workflow from template '{Name}': {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Validates that all required parameters are provided and have correct types.
    /// </summary>
    /// <param name="parameters">The parameters to validate.</param>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public void ValidateParameters(WorkflowTemplateParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var missingParameters = new List<string>();
        var invalidParameters = new List<string>();

        foreach (var (key, paramDef) in RequiredParameters)
        {
            if (!parameters.TryGetValue(key, out var value))
            {
                if (paramDef.IsRequired)
                {
                    missingParameters.Add(key);
                }
                continue;
            }

            // Validate type
            if (value != null && paramDef.ParameterType != null)
            {
                var valueType = value.GetType();
                if (!paramDef.ParameterType.IsAssignableFrom(valueType))
                {
                    invalidParameters.Add($"{key} (expected {paramDef.ParameterType.Name}, got {valueType.Name})");
                }
            }
        }

        if (missingParameters.Count > 0 || invalidParameters.Count > 0)
        {
            var errors = new List<string>();
            if (missingParameters.Count > 0)
            {
                errors.Add($"Missing required parameters: {string.Join(", ", missingParameters)}");
            }
            if (invalidParameters.Count > 0)
            {
                errors.Add($"Invalid parameter types: {string.Join(", ", invalidParameters)}");
            }

            throw new ArgumentException(
                $"Template '{Name}' parameter validation failed: {string.Join("; ", errors)}",
                nameof(parameters));
        }
    }
}

/// <summary>
/// Represents a collection of workflow template parameters.
/// </summary>
public class WorkflowTemplateParameters : Dictionary<string, object?>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowTemplateParameters"/> class.
    /// </summary>
    public WorkflowTemplateParameters()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowTemplateParameters"/> class with initial parameters.
    /// </summary>
    /// <param name="parameters">Initial parameters.</param>
    public WorkflowTemplateParameters(IDictionary<string, object?> parameters)
        : base(parameters)
    {
    }

    /// <summary>
    /// Gets a parameter value with type conversion.
    /// </summary>
    /// <typeparam name="T">The expected type.</typeparam>
    /// <param name="key">The parameter key.</param>
    /// <param name="defaultValue">Optional default value if parameter is not found.</param>
    /// <returns>The parameter value, or default value if not found.</returns>
    public T? GetValue<T>(string key, T? defaultValue = default)
    {
        if (!TryGetValue(key, out var value))
        {
            return defaultValue;
        }

        if (value is T typedValue)
        {
            return typedValue;
        }

        if (value == null)
        {
            return defaultValue;
        }

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }
}

/// <summary>
/// Represents a template parameter definition.
/// </summary>
public class TemplateParameter
{
    /// <summary>
    /// Gets the parameter name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the parameter type.
    /// </summary>
    public Type? ParameterType { get; init; }

    /// <summary>
    /// Gets whether this parameter is required.
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    /// Gets the parameter description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the default value for this parameter.
    /// </summary>
    public object? DefaultValue { get; init; }
}
