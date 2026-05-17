using DotNetAgents.Workflow.Graph;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Workflow.Templates;

/// <summary>
/// Builder for creating workflow templates.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public class WorkflowTemplateBuilder<TState> where TState : class
{
    private string? _name;
    private string? _description;
    private string _version = "1.0.0";
    private readonly Dictionary<string, TemplateParameter> _parameters = new();
    private Func<WorkflowTemplateParameters, StateGraph<TState>>? _workflowFactory;
    private ILogger<WorkflowTemplate<TState>>? _logger;

    /// <summary>
    /// Sets the template name.
    /// </summary>
    /// <param name="name">The template name.</param>
    /// <returns>The builder for chaining.</returns>
    public WorkflowTemplateBuilder<TState> WithName(string name)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        return this;
    }

    /// <summary>
    /// Sets the template description.
    /// </summary>
    /// <param name="description">The template description.</param>
    /// <returns>The builder for chaining.</returns>
    public WorkflowTemplateBuilder<TState> WithDescription(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the template version.
    /// </summary>
    /// <param name="version">The template version.</param>
    /// <returns>The builder for chaining.</returns>
    public WorkflowTemplateBuilder<TState> WithVersion(string version)
    {
        _version = version ?? throw new ArgumentNullException(nameof(version));
        return this;
    }

    /// <summary>
    /// Adds a required parameter to the template.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="parameterType">The parameter type.</param>
    /// <param name="description">Optional parameter description.</param>
    /// <returns>The builder for chaining.</returns>
    public WorkflowTemplateBuilder<TState> AddRequiredParameter(
        string name,
        Type parameterType,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(parameterType);

        _parameters[name] = new TemplateParameter
        {
            Name = name,
            ParameterType = parameterType,
            IsRequired = true,
            Description = description
        };

        return this;
    }

    /// <summary>
    /// Adds an optional parameter to the template.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="parameterType">The parameter type.</param>
    /// <param name="defaultValue">Optional default value.</param>
    /// <param name="description">Optional parameter description.</param>
    /// <returns>The builder for chaining.</returns>
    public WorkflowTemplateBuilder<TState> AddOptionalParameter(
        string name,
        Type parameterType,
        object? defaultValue = null,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(parameterType);

        _parameters[name] = new TemplateParameter
        {
            Name = name,
            ParameterType = parameterType,
            IsRequired = false,
            DefaultValue = defaultValue,
            Description = description
        };

        return this;
    }

    /// <summary>
    /// Sets the workflow factory function.
    /// </summary>
    /// <param name="factory">A function that creates a workflow graph from template parameters.</param>
    /// <returns>The builder for chaining.</returns>
    public WorkflowTemplateBuilder<TState> WithWorkflowFactory(
        Func<WorkflowTemplateParameters, StateGraph<TState>> factory)
    {
        _workflowFactory = factory ?? throw new ArgumentNullException(nameof(factory));
        return this;
    }

    /// <summary>
    /// Sets the logger instance.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <returns>The builder for chaining.</returns>
    public WorkflowTemplateBuilder<TState> WithLogger(ILogger<WorkflowTemplate<TState>> logger)
    {
        _logger = logger;
        return this;
    }

    /// <summary>
    /// Builds the workflow template.
    /// </summary>
    /// <returns>The created workflow template.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required properties are not set.</exception>
    public WorkflowTemplate<TState> Build()
    {
        if (string.IsNullOrWhiteSpace(_name))
        {
            throw new InvalidOperationException("Template name is required.");
        }

        if (_workflowFactory == null)
        {
            throw new InvalidOperationException("Workflow factory is required.");
        }

        return new WorkflowTemplate<TState>(
            _name,
            _workflowFactory,
            _parameters,
            _description,
            _version,
            _logger);
    }
}
