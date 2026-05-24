// SPDX-License-Identifier: Apache-2.0

using System.Text;

using DotNetAgents.Abstractions.Documents;



/// <summary>
/// Loads Markdown documents from file system or string content.
/// </summary>
public class MarkdownDocumentLoader : IDocumentLoader
{
    /// <summary>
    /// Loads a Markdown document from a file path or string content.
    /// </summary>
    /// <param name="source">The file path or Markdown content to load.</param>
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

        // Treat as raw Markdown content
        return new[]
        {
            new Document
            {
                Content = source,
                Metadata = new Dictionary<string, object>
                {
                    ["source"] = "inline",
                    ["type"] = "markdown"
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

        return new[]
        {
            new Document
            {
                Content = content,
                Metadata = new Dictionary<string, object>
                {
                    ["source"] = filePath,
                    ["filename"] = fileName,
                    ["type"] = "markdown",
                    ["extension"] = Path.GetExtension(filePath)
                }
            }
        };
    }
}
