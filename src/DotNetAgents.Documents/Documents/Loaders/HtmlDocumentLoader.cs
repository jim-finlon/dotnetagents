using System.Text;
using DotNetAgents.Abstractions.Documents;
using HtmlAgilityPack;

namespace DotNetAgents.Documents.Loaders;

/// <summary>
/// Loads HTML documents from file system, URLs, or string content using HtmlAgilityPack.
/// </summary>
public class HtmlDocumentLoader : IDocumentLoader
{
    /// <summary>
    /// Gets or sets whether to extract text only (strip HTML tags).
    /// Default is true.
    /// </summary>
    public bool ExtractTextOnly { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to preserve links in the extracted text.
    /// Default is true.
    /// </summary>
    public bool PreserveLinks { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to split HTML into separate documents per element (div, article, section).
    /// Default is false (single document with all content).
    /// </summary>
    public bool SplitByElement { get; set; } = false;

    /// <summary>
    /// Gets or sets the element tags to split by when SplitByElement is true.
    /// Default: ["article", "section", "div"].
    /// </summary>
    public string[] SplitElementTags { get; set; } = ["article", "section", "div"];

    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlDocumentLoader"/> class.
    /// </summary>
    /// <param name="extractTextOnly">Whether to extract text only (strip HTML tags). Default is true.</param>
    /// <param name="preserveLinks">Whether to preserve links in the extracted text. Default is true.</param>
    /// <param name="splitByElement">Whether to split HTML into separate documents per element. Default is false.</param>
    public HtmlDocumentLoader(
        bool extractTextOnly = true,
        bool preserveLinks = true,
        bool splitByElement = false)
    {
        ExtractTextOnly = extractTextOnly;
        PreserveLinks = preserveLinks;
        SplitByElement = splitByElement;
    }

    /// <summary>
    /// Loads an HTML document from a file path, URL, or string content.
    /// </summary>
    /// <param name="source">The file path, URL, or HTML content to load.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of documents.</returns>
    public async Task<IReadOnlyList<Document>> LoadAsync(
        string source,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be null or whitespace.", nameof(source));

        HtmlDocument htmlDoc;

        // Check if source is a file path
        if (File.Exists(source))
        {
            htmlDoc = await LoadFromFileAsync(source, cancellationToken).ConfigureAwait(false);
            return await ProcessHtmlDocumentAsync(htmlDoc, source, cancellationToken).ConfigureAwait(false);
        }

        // Check if source is a URL
        if (Uri.TryCreate(source, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            htmlDoc = await LoadFromUrlAsync(source, cancellationToken).ConfigureAwait(false);
            return await ProcessHtmlDocumentAsync(htmlDoc, source, cancellationToken).ConfigureAwait(false);
        }

        // Treat as raw HTML content
        htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(source);
        return await ProcessHtmlDocumentAsync(htmlDoc, "inline", cancellationToken).ConfigureAwait(false);
    }

    private async Task<HtmlDocument> LoadFromFileAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(content);
        return htmlDoc;
    }

    private async Task<HtmlDocument> LoadFromUrlAsync(
        string url,
        CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        var htmlContent = await httpClient.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(htmlContent);
        return htmlDoc;
    }

    private Task<IReadOnlyList<Document>> ProcessHtmlDocumentAsync(
        HtmlDocument htmlDoc,
        string source,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var documents = new List<Document>();
        var baseMetadata = new Dictionary<string, object>
        {
            ["source"] = source,
            ["type"] = "html"
        };

        // Extract title if available
        var titleNode = htmlDoc.DocumentNode.SelectSingleNode("//title");
        if (titleNode != null)
        {
            baseMetadata["title"] = titleNode.InnerText.Trim();
        }

        // Extract meta tags
        var metaTags = htmlDoc.DocumentNode.SelectNodes("//meta");
        if (metaTags != null)
        {
            foreach (var meta in metaTags)
            {
                var name = meta.GetAttributeValue("name", "") ?? meta.GetAttributeValue("property", "");
                var content = meta.GetAttributeValue("content", "");
                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(content))
                {
                    baseMetadata[$"meta_{name}"] = content;
                }
            }
        }

        if (SplitByElement)
        {
            // Split by specified elements
            foreach (var tag in SplitElementTags)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var elements = htmlDoc.DocumentNode.SelectNodes($"//{tag}");
                if (elements == null)
                    continue;

                foreach (var element in elements)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var content = ExtractContent(element);
                    if (string.IsNullOrWhiteSpace(content))
                        continue;

                    var metadata = new Dictionary<string, object>(baseMetadata)
                    {
                        ["element_tag"] = tag,
                        ["element_id"] = element.GetAttributeValue("id", ""),
                        ["element_class"] = element.GetAttributeValue("class", "")
                    };

                    documents.Add(new Document
                    {
                        Content = content,
                        Metadata = metadata
                    });
                }
            }
        }
        else
        {
            // Single document with all content
            var content = ExtractContent(htmlDoc.DocumentNode);
            documents.Add(new Document
            {
                Content = content,
                Metadata = baseMetadata
            });
        }

        return Task.FromResult<IReadOnlyList<Document>>(documents);
    }

    private string ExtractContent(HtmlNode node)
    {
        if (ExtractTextOnly)
        {
            var textBuilder = new StringBuilder();

            if (PreserveLinks)
            {
                // Extract text and preserve links as [text](url) format
                foreach (var link in node.SelectNodes(".//a") ?? new HtmlNodeCollection(node))
                {
                    var linkText = link.InnerText.Trim();
                    var href = link.GetAttributeValue("href", "");
                    if (!string.IsNullOrWhiteSpace(linkText))
                    {
                        if (!string.IsNullOrWhiteSpace(href))
                        {
                            textBuilder.Append($"[{linkText}]({href}) ");
                        }
                        else
                        {
                            textBuilder.Append(linkText + " ");
                        }
                    }
                }

                // Remove link nodes to avoid duplicate text
                var clonedNode = node.CloneNode(true);
                foreach (var link in clonedNode.SelectNodes(".//a") ?? new HtmlNodeCollection(clonedNode))
                {
                    link.Remove();
                }

                var remainingText = clonedNode.InnerText;
                if (!string.IsNullOrWhiteSpace(remainingText))
                {
                    textBuilder.Append(remainingText);
                }
            }
            else
            {
                textBuilder.Append(node.InnerText);
            }

            return textBuilder.ToString().Trim();
        }

        // Return HTML content
        return node.InnerHtml;
    }
}
