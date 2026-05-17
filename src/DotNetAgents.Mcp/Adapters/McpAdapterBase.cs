using DotNetAgents.Mcp.Models;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Mcp.Adapters;

/// <summary>
/// Base class for MCP adapters that provides common functionality.
/// </summary>
public abstract class McpAdapterBase : IMcpAdapter
{
    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Gets the dictionary of tool handlers keyed by tool name.
    /// </summary>
    protected Dictionary<string, Func<Dictionary<string, object>, CancellationToken, Task<object>>> ToolHandlers { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="McpAdapterBase"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    protected McpAdapterBase(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ToolHandlers = new Dictionary<string, Func<Dictionary<string, object>, CancellationToken, Task<object>>>();
        RegisterToolHandlers();
    }

    /// <summary>
    /// Registers tool handlers. Override this method to register your tool handlers.
    /// </summary>
    protected abstract void RegisterToolHandlers();

    /// <inheritdoc />
    public virtual async Task<object> ExecuteToolAsync(
        string toolName,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            throw new ArgumentException("Tool name cannot be null or empty", nameof(toolName));
        }

        ArgumentNullException.ThrowIfNull(parameters);

        if (!ToolHandlers.TryGetValue(toolName, out var handler))
        {
            Logger.LogWarning("Tool {ToolName} is not supported by adapter {AdapterType}", toolName, GetType().Name);
            throw new NotSupportedException($"Tool '{toolName}' is not supported by this adapter");
        }

        try
        {
            Logger.LogDebug("Executing tool {ToolName} with adapter {AdapterType}", toolName, GetType().Name);
            var result = await handler(parameters, cancellationToken).ConfigureAwait(false);
            Logger.LogDebug("Tool {ToolName} executed successfully", toolName);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing tool {ToolName} with adapter {AdapterType}", toolName, GetType().Name);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual IEnumerable<string> GetSupportedTools()
    {
        return ToolHandlers.Keys;
    }

    /// <summary>
    /// Validates that required parameters are present.
    /// </summary>
    /// <param name="parameters">The parameters dictionary.</param>
    /// <param name="requiredParams">The list of required parameter names.</param>
    /// <exception cref="ArgumentException">Thrown when required parameters are missing.</exception>
    protected static void ValidateRequiredParameters(
        Dictionary<string, object> parameters,
        params string[] requiredParams)
    {
        var missing = requiredParams.Where(p => !parameters.ContainsKey(p)).ToList();
        if (missing.Count > 0)
        {
            throw new ArgumentException(
                $"Missing required parameters: {string.Join(", ", missing)}",
                nameof(parameters));
        }
    }

    /// <summary>
    /// Gets a parameter value with type conversion.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="parameters">The parameters dictionary.</param>
    /// <param name="key">The parameter key.</param>
    /// <param name="defaultValue">Optional default value if parameter is missing.</param>
    /// <returns>The parameter value, or default if not found.</returns>
    protected static T? GetParameter<T>(
        Dictionary<string, object> parameters,
        string key,
        T? defaultValue = default)
    {
        if (!parameters.TryGetValue(key, out var value))
        {
            return defaultValue;
        }

        if (value is T directValue)
        {
            return directValue;
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
/// Interface for MCP adapters that execute tools.
/// </summary>
public interface IMcpAdapter
{
    /// <summary>
    /// Executes a tool with the provided parameters.
    /// </summary>
    /// <param name="toolName">The name of the tool to execute.</param>
    /// <param name="parameters">The tool parameters.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The tool execution result.</returns>
    Task<object> ExecuteToolAsync(
        string toolName,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of tool names supported by this adapter.
    /// </summary>
    /// <returns>The list of supported tool names.</returns>
    IEnumerable<string> GetSupportedTools();
}
