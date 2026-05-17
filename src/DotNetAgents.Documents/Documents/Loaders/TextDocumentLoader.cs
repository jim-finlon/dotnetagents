using System.Text;

using DotNetAgents.Abstractions.Documents;



/// <summary>
/// Loads text documents from file system or string content.
/// </summary>
public class TextDocumentLoader : IDocumentLoader
{
    /// <summary>
    /// Loads a text document from a file path.
    /// </summary>
    /// <param name="source">The file path to load.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list containing the loaded document.</returns>
    public async Task<IReadOnlyList<Document>> LoadAsync(
        string source,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be null or whitespace.", nameof(source));

        // Check if source is a file path or raw content
        if (File.Exists(source))
        {
            return await LoadFromFileAsync(source, cancellationToken).ConfigureAwait(false);
        }

        // Treat as raw content
        return new[]
        {
            new Document
            {
                Content = source,
                Metadata = new Dictionary<string, object>
                {
                    ["source"] = "inline",
                    ["type"] = "text"
                }
            }
        };
    }

    private static async Task<IReadOnlyList<Document>> LoadFromFileAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
        var fileName = Path.GetFileName(filePath);
        var fileExtension = Path.GetExtension(filePath);

        return new[]
        {
            new Document
            {
                Content = content,
                Metadata = new Dictionary<string, object>
                {
                    ["source"] = filePath,
                    ["filename"] = fileName,
                    ["extension"] = fileExtension,
                    ["type"] = "text"
                }
            }
        };
    }
}
