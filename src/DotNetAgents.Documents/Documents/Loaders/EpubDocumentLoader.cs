// SPDX-License-Identifier: Apache-2.0

using System.Text;
using VersOne.Epub;

using DotNetAgents.Abstractions.Documents;



/// <summary>
/// Loads EPUB documents from file system.
/// </summary>
public class EpubDocumentLoader : IDocumentLoader
{
    /// <summary>
    /// Gets or sets whether to split by chapter. Default is true (one document per chapter).
    /// </summary>
    public bool SplitByChapter { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="EpubDocumentLoader"/> class.
    /// </summary>
    /// <param name="splitByChapter">Whether to split by chapter. Default is true.</param>
    public EpubDocumentLoader(bool splitByChapter = true)
    {
        SplitByChapter = splitByChapter;
    }

    /// <summary>
    /// Loads an EPUB document from a file path.
    /// </summary>
    /// <param name="source">The file path to the EPUB file.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of documents, one per chapter if SplitByChapter is true, otherwise a single document.</returns>
    public Task<IReadOnlyList<Document>> LoadAsync(
        string source,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be null or whitespace.", nameof(source));

        if (!File.Exists(source))
            throw new FileNotFoundException($"EPUB file not found: {source}", source);

        if (!source.EndsWith(".epub", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Source must be an EPUB file (.epub).", nameof(source));

        return LoadFromFileAsync(source, cancellationToken);
    }

    private async Task<IReadOnlyList<Document>> LoadFromFileAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var documents = new List<Document>();
        var fileName = Path.GetFileName(filePath);
        var fileInfo = new FileInfo(filePath);

        try
        {
            var epubBook = await EpubReader.ReadBookAsync(filePath).ConfigureAwait(false);

            var baseMetadata = new Dictionary<string, object>
            {
                ["source"] = filePath,
                ["filename"] = fileName,
                ["type"] = "epub",
                ["file_size"] = fileInfo.Length,
                ["created_at"] = fileInfo.CreationTimeUtc
            };

            // Add EPUB metadata
            if (!string.IsNullOrWhiteSpace(epubBook.Title))
                baseMetadata["title"] = epubBook.Title;

            // Note: Additional metadata (Author, Publisher, etc.) can be accessed via epubBook.Content
            // For now, we focus on extracting the text content

            if (SplitByChapter)
            {
                // Create one document per chapter
                var chapters = epubBook.ReadingOrder;
                int chapterIndex = 0;

                foreach (var chapter in chapters)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var chapterContent = await ExtractChapterContentAsync(chapter, cancellationToken).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(chapterContent))
                        continue;

                    var chapterMetadata = new Dictionary<string, object>(baseMetadata)
                    {
                        ["chapter_title"] = string.IsNullOrEmpty(chapter.Key)
                            ? $"Chapter {chapterIndex + 1}"
                            : chapter.Key,
                        ["chapter_number"] = chapterIndex + 1,
                        ["chapter_id"] = chapter.Key ?? string.Empty
                    };

                    documents.Add(new Document
                    {
                        Content = chapterContent,
                        PageNumber = chapterIndex + 1,
                        Metadata = chapterMetadata
                    });

                    chapterIndex++;
                }
            }
            else
            {
                // Combine all chapters into a single document
                var allContent = new StringBuilder();
                var chapters = epubBook.ReadingOrder;
                int chapterIndex = 0;

                foreach (var chapter in chapters)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var chapterContent = await ExtractChapterContentAsync(chapter, cancellationToken).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(chapterContent))
                        continue;

                    allContent.AppendLine($"--- Chapter {chapterIndex + 1} ---");
                    allContent.AppendLine(chapterContent);
                    allContent.AppendLine();
                    chapterIndex++;
                }

                baseMetadata["chapter_count"] = chapterIndex;

                documents.Add(new Document
                {
                    Content = allContent.ToString().TrimEnd(),
                    Metadata = baseMetadata
                });
            }

            return documents;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException($"Failed to load EPUB file: {filePath}", ex);
        }
    }

    private static async Task<string> ExtractChapterContentAsync(EpubLocalTextContentFile chapter, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // VersOne.Epub provides Content property directly
            var htmlContent = chapter.Content;
            if (string.IsNullOrWhiteSpace(htmlContent))
            {
                // Try async read if Content is not available
                htmlContent = await Task.Run(() => chapter.Content, cancellationToken).ConfigureAwait(false);
            }

            if (string.IsNullOrWhiteSpace(htmlContent))
                return string.Empty;

            // Extract text from HTML
            var textContent = ExtractTextFromHtml(htmlContent);
            return textContent;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return string.Empty;
        }
    }

    private static string ExtractTextFromHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var textBuilder = new StringBuilder();
        var inScript = false;
        var inStyle = false;

        try
        {
            using var reader = new StringReader(html);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();

                // Skip script and style tags
                if (line.Contains("<script", StringComparison.OrdinalIgnoreCase))
                    inScript = true;
                if (line.Contains("</script>", StringComparison.OrdinalIgnoreCase))
                    inScript = false;
                if (line.Contains("<style", StringComparison.OrdinalIgnoreCase))
                    inStyle = true;
                if (line.Contains("</style>", StringComparison.OrdinalIgnoreCase))
                    inStyle = false;

                if (inScript || inStyle)
                    continue;

                // Remove HTML tags using simple regex-like approach
                var text = RemoveHtmlTags(line);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    textBuilder.AppendLine(text);
                }
            }
        }
        catch
        {
            // Fallback: simple tag removal
            return RemoveHtmlTags(html);
        }

        return textBuilder.ToString().Trim();
    }

    private static string RemoveHtmlTags(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var result = new StringBuilder();
        var inTag = false;

        foreach (var c in html)
        {
            if (c == '<')
            {
                inTag = true;
            }
            else if (c == '>')
            {
                inTag = false;
            }
            else if (!inTag)
            {
                result.Append(c);
            }
        }

        // Decode HTML entities
        var text = result.ToString();
        text = System.Net.WebUtility.HtmlDecode(text);

        // Clean up whitespace
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line));

        return string.Join(Environment.NewLine, lines);
    }
}
