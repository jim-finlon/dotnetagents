using System.Text.Json;

namespace DotNetAgents.Abstractions.Tools;

/// <summary>
/// Interface for tools that can be executed by agents.
/// </summary>
public interface ITool
{
    /// <summary>
    /// Gets the name of the tool.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of what the tool does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the JSON schema describing the input parameters.
    /// </summary>
    JsonElement InputSchema { get; }

    /// <summary>
    /// Executes the tool with the given input.
    /// </summary>
    /// <param name="input">The input parameters for the tool.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The result of the tool execution.</returns>
    Task<ToolResult> ExecuteAsync(
        object input,
        CancellationToken cancellationToken = default);
}
