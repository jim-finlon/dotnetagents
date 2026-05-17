namespace DotNetAgents.Abstractions.Documents;

/// <summary>
/// Interface for loading documents from various sources.
/// </summary>
public interface IDocumentLoader
{
    /// <summary>
    /// Loads documents from the specified source.
    /// </summary>
    /// <param name="source">The source identifier (file path, URL, etc.).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of loaded documents.</returns>
    Task<IReadOnlyList<Document>> LoadAsync(
        string source,
        CancellationToken cancellationToken = default);
}
