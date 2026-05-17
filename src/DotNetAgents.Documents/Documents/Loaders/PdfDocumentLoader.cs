using System.Text;
using DotNetAgents.Abstractions.Documents;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;


namespace DotNetAgents.Documents.Loaders;

/// <summary>
/// Loads PDF documents from file system using PdfPig library.
/// </summary>
public class PdfDocumentLoader : IDocumentLoader
{
    /// <summary>
    /// Gets or sets whether to split PDF into separate documents per page.
    /// Default is true (one document per page).
    /// </summary>
    public bool SplitByPage { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfDocumentLoader"/> class.
    /// </summary>
    /// <param name="splitByPage">Whether to split PDF into separate documents per page. Default is true.</param>
    public PdfDocumentLoader(bool splitByPage = true)
    {
        SplitByPage = splitByPage;
    }

    /// <summary>
    /// Loads a PDF document from a file path.
    /// </summary>
    /// <param name="source">The file path to the PDF file.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of documents, one per page if SplitByPage is true, otherwise a single document.</returns>
    public Task<IReadOnlyList<Document>> LoadAsync(
        string source,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be null or whitespace.", nameof(source));

        if (!File.Exists(source))
            throw new FileNotFoundException($"PDF file not found: {source}", source);

        if (!source.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Source must be a PDF file.", nameof(source));

        return LoadFromFileAsync(source, cancellationToken);
    }

    private Task<IReadOnlyList<Document>> LoadFromFileAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var documents = new List<Document>();
            var fileName = Path.GetFileName(filePath);
            var fileInfo = new FileInfo(filePath);

            using var document = PdfDocument.Open(filePath);
            var totalPages = document.NumberOfPages;

            if (SplitByPage)
            {
                // Create one document per page
                for (int pageNumber = 1; pageNumber <= totalPages; pageNumber++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var page = document.GetPage(pageNumber);
                    var text = ExtractTextFromPage(page);

                    documents.Add(new Document
                    {
                        Content = text,
                        PageNumber = pageNumber,
                        Metadata = new Dictionary<string, object>
                        {
                            ["source"] = filePath,
                            ["filename"] = fileName,
                            ["type"] = "pdf",
                            ["page"] = pageNumber,
                            ["total_pages"] = totalPages,
                            ["file_size"] = fileInfo.Length,
                            ["created_at"] = fileInfo.CreationTimeUtc
                        }
                    });
                }
            }
            else
            {
                // Combine all pages into a single document
                var allText = new StringBuilder();
                for (int pageNumber = 1; pageNumber <= totalPages; pageNumber++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var page = document.GetPage(pageNumber);
                    var pageText = ExtractTextFromPage(page);
                    allText.AppendLine($"--- Page {pageNumber} ---");
                    allText.AppendLine(pageText);
                    allText.AppendLine();
                }

                documents.Add(new Document
                {
                    Content = allText.ToString(),
                    Metadata = new Dictionary<string, object>
                    {
                        ["source"] = filePath,
                        ["filename"] = fileName,
                        ["type"] = "pdf",
                        ["page_count"] = totalPages,
                        ["file_size"] = fileInfo.Length,
                        ["created_at"] = fileInfo.CreationTimeUtc
                    }
                });
            }

            return Task.FromResult<IReadOnlyList<Document>>(documents);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException($"Failed to load PDF file: {filePath}", ex);
        }
    }

    private static string ExtractTextFromPage(Page page)
    {
        var textBuilder = new StringBuilder();

        foreach (var word in page.GetWords())
        {
            textBuilder.Append(word.Text);
            textBuilder.Append(' ');
        }

        return textBuilder.ToString().Trim();
    }
}
