namespace DotNetAgents.Abstractions.Documents;

/// <summary>
/// Interface for splitting documents into chunks.
/// </summary>
public interface ITextSplitter
{
    /// <summary>
    /// Splits a document into chunks.
    /// </summary>
    /// <param name="document">The document to split.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of document chunks.</returns>
    Task<IReadOnlyList<Document>> SplitAsync(
        Document document,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Splits multiple documents into chunks.
    /// </summary>
    /// <param name="documents">The documents to split.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of document chunks.</returns>
    Task<IReadOnlyList<Document>> SplitDocumentsAsync(
        IEnumerable<Document> documents,
        CancellationToken cancellationToken = default);
}
