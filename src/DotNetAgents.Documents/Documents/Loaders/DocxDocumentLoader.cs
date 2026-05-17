using System.Text;
using DotNetAgents.Abstractions.Documents;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Document = DotNetAgents.Abstractions.Documents.Document;

namespace DotNetAgents.Documents.Loaders;

/// <summary>
/// Loads DOCX (Word) documents from file system using DocumentFormat.OpenXml library.
/// </summary>
public class DocxDocumentLoader : IDocumentLoader
{
    /// <summary>
    /// Gets or sets whether to split DOCX into separate documents per paragraph.
    /// Default is false (single document with all content).
    /// </summary>
    public bool SplitByParagraph { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to preserve formatting metadata.
    /// Default is false.
    /// </summary>
    public bool PreserveFormatting { get; set; } = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocxDocumentLoader"/> class.
    /// </summary>
    /// <param name="splitByParagraph">Whether to split DOCX into separate documents per paragraph. Default is false.</param>
    /// <param name="preserveFormatting">Whether to preserve formatting metadata. Default is false.</param>
    public DocxDocumentLoader(bool splitByParagraph = false, bool preserveFormatting = false)
    {
        SplitByParagraph = splitByParagraph;
        PreserveFormatting = preserveFormatting;
    }

    /// <summary>
    /// Loads a DOCX document from a file path.
    /// </summary>
    /// <param name="source">The file path to the DOCX file.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of documents, one per paragraph if SplitByParagraph is true, otherwise a single document.</returns>
    public Task<IReadOnlyList<Document>> LoadAsync(
        string source,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be null or whitespace.", nameof(source));

        if (!File.Exists(source))
            throw new FileNotFoundException($"DOCX file not found: {source}", source);

        if (!source.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Source must be a DOCX file.", nameof(source));

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

            using var wordDocument = WordprocessingDocument.Open(filePath, false);
            var body = wordDocument.MainDocumentPart?.Document?.Body;
            if (body == null)
            {
                return Task.FromResult<IReadOnlyList<Document>>(new[]
                {
                    new Document
                    {
                        Content = string.Empty,
                        Metadata = new Dictionary<string, object>
                        {
                            ["source"] = filePath,
                            ["filename"] = fileName,
                            ["type"] = "docx",
                            ["file_size"] = fileInfo.Length,
                            ["created_at"] = fileInfo.CreationTimeUtc
                        }
                    }
                });
            }

            var paragraphs = body.Elements<Paragraph>().ToList();
            var allText = new StringBuilder();

            if (SplitByParagraph)
            {
                // Create one document per paragraph
                for (int i = 0; i < paragraphs.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var paragraph = paragraphs[i];
                    var text = ExtractTextFromParagraph(paragraph);

                    if (string.IsNullOrWhiteSpace(text))
                        continue;

                    var metadata = new Dictionary<string, object>
                    {
                        ["source"] = filePath,
                        ["filename"] = fileName,
                        ["type"] = "docx",
                        ["paragraph_number"] = i + 1,
                        ["total_paragraphs"] = paragraphs.Count,
                        ["file_size"] = fileInfo.Length,
                        ["created_at"] = fileInfo.CreationTimeUtc
                    };

                    if (PreserveFormatting)
                    {
                        AddFormattingMetadata(paragraph, metadata);
                    }

                    documents.Add(new Document
                    {
                        Content = text,
                        Metadata = metadata
                    });
                }
            }
            else
            {
                // Combine all paragraphs into a single document
                foreach (var paragraph in paragraphs)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var text = ExtractTextFromParagraph(paragraph);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        allText.AppendLine(text);
                    }
                }

                var metadata = new Dictionary<string, object>
                {
                    ["source"] = filePath,
                    ["filename"] = fileName,
                    ["type"] = "docx",
                    ["paragraph_count"] = paragraphs.Count,
                    ["file_size"] = fileInfo.Length,
                    ["created_at"] = fileInfo.CreationTimeUtc
                };

                documents.Add(new Document
                {
                    Content = allText.ToString().Trim(),
                    Metadata = metadata
                });
            }

            return Task.FromResult<IReadOnlyList<Document>>(documents);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException($"Failed to load DOCX file: {filePath}", ex);
        }
    }

    private static string ExtractTextFromParagraph(Paragraph paragraph)
    {
        var textBuilder = new StringBuilder();

        foreach (var text in paragraph.Descendants<Text>())
        {
            textBuilder.Append(text.Text);
        }

        return textBuilder.ToString().Trim();
    }

    private static void AddFormattingMetadata(Paragraph paragraph, Dictionary<string, object> metadata)
    {
        var runProperties = paragraph.Descendants<RunProperties>().FirstOrDefault();
        if (runProperties != null)
        {
            if (runProperties.Bold != null)
                metadata["bold"] = true;
            if (runProperties.Italic != null)
                metadata["italic"] = true;
            if (runProperties.Underline != null)
                metadata["underline"] = true;

            var fontSizeValue = runProperties.FontSize?.Val?.Value;
            if (fontSizeValue != null && uint.TryParse(fontSizeValue.ToString(), out var fontSize))
                metadata["font_size"] = fontSize / 2.0; // Convert from half-points to points
        }

        var paragraphProperties = paragraph.ParagraphProperties;
        if (paragraphProperties != null)
        {
            var justification = paragraphProperties.Justification?.Val?.Value;
            if (justification != null)
                metadata["alignment"] = justification.ToString();
        }
    }
}
