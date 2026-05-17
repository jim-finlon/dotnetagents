namespace DotNetAgents.Abstractions.Tools;

/// <summary>
/// Represents the result of a tool execution.
/// </summary>
public record ToolResult
{
    /// <summary>
    /// Gets or sets the output of the tool execution.
    /// </summary>
    public object? Output { get; init; }

    /// <summary>
    /// Gets or sets whether the tool execution was successful.
    /// </summary>
    public bool IsSuccess { get; init; } = true;

    /// <summary>
    /// Gets or sets an error message if the execution failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets or sets metadata about the tool execution.
    /// </summary>
    public IDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Creates a successful tool result.
    /// </summary>
    /// <param name="output">The output value.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <returns>A successful tool result.</returns>
    public static ToolResult Success(object? output, IDictionary<string, object>? metadata = null) =>
        new() { Output = output, IsSuccess = true, Metadata = metadata };

    /// <summary>
    /// Creates a failed tool result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <returns>A failed tool result.</returns>
    public static ToolResult Failure(string errorMessage, IDictionary<string, object>? metadata = null) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage, Metadata = metadata };
}
