namespace DotNetAgents.AgentFramework.Adapters;

/// <summary>
/// Adapter interface for exposing DotNetAgents document loaders as Microsoft Agent Framework tools.
/// </summary>
/// <remarks>
/// This interface will be implemented when Microsoft Agent Framework APIs stabilize.
/// It provides a bridge between DotNetAgents document loaders and MAF tool system.
/// </remarks>
public interface IDocumentLoaderTool
{
    /// <summary>
    /// Gets the name of the document loader tool.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of what this tool does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the input schema for the tool (file path, content, options).
    /// </summary>
    object InputSchema { get; }

    /// <summary>
    /// Executes the document loader and returns loaded documents.
    /// </summary>
    /// <param name="input">The input parameters (file path, content, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded documents.</returns>
    Task<object> ExecuteAsync(object input, CancellationToken cancellationToken = default);
}
