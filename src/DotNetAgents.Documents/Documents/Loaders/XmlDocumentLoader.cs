using System.Text;
using System.Xml;
using System.Xml.Linq;
using DotNetAgents.Abstractions.Documents;

namespace DotNetAgents.Documents.Loaders;

/// <summary>
/// Loads XML documents from file system or string content.
/// </summary>
public class XmlDocumentLoader : IDocumentLoader
{
    /// <summary>
    /// Gets or sets whether to extract text only (strip XML tags).
    /// Default is true.
    /// </summary>
    public bool ExtractTextOnly { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to preserve XML structure metadata.
    /// Default is true.
    /// </summary>
    public bool PreserveStructure { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to split XML into separate documents per element.
    /// Default is false (single document with all content).
    /// </summary>
    public bool SplitByElement { get; set; } = false;

    /// <summary>
    /// Gets or sets the element names to split by when SplitByElement is true.
    /// If empty, splits by all elements at the root level.
    /// </summary>
    public string[]? SplitElementNames { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlDocumentLoader"/> class.
    /// </summary>
    /// <param name="extractTextOnly">Whether to extract text only (strip XML tags). Default is true.</param>
    /// <param name="preserveStructure">Whether to preserve XML structure metadata. Default is true.</param>
    /// <param name="splitByElement">Whether to split XML into separate documents per element. Default is false.</param>
    public XmlDocumentLoader(
        bool extractTextOnly = true,
        bool preserveStructure = true,
        bool splitByElement = false)
    {
        ExtractTextOnly = extractTextOnly;
        PreserveStructure = preserveStructure;
        SplitByElement = splitByElement;
    }

    /// <summary>
    /// Loads an XML document from a file path or string content.
    /// </summary>
    /// <param name="source">The file path or XML content to load.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of documents.</returns>
    public async Task<IReadOnlyList<Document>> LoadAsync(
        string source,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be null or whitespace.", nameof(source));

        XDocument xmlDoc;
        string? filePath = null;

        // Check if source is a file path
        if (File.Exists(source))
        {
            filePath = source;
            var content = await File.ReadAllTextAsync(source, cancellationToken).ConfigureAwait(false);
            xmlDoc = XDocument.Parse(content);
        }
        else
        {
            // Treat as raw XML content
            xmlDoc = XDocument.Parse(source);
        }

        return ProcessXmlDocument(xmlDoc, filePath, cancellationToken);
    }

    private IReadOnlyList<Document> ProcessXmlDocument(
        XDocument xmlDoc,
        string? filePath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var documents = new List<Document>();
        var baseMetadata = new Dictionary<string, object>
        {
            ["type"] = "xml"
        };

        if (filePath != null)
        {
            var fileInfo = new FileInfo(filePath);
            baseMetadata["source"] = filePath;
            baseMetadata["filename"] = fileInfo.Name;
            baseMetadata["file_size"] = fileInfo.Length;
            baseMetadata["created_at"] = fileInfo.CreationTimeUtc;
        }
        else
        {
            baseMetadata["source"] = "inline";
        }

        // Extract root element metadata
        var root = xmlDoc.Root;
        if (root != null)
        {
            if (PreserveStructure)
            {
                baseMetadata["root_element"] = root.Name.LocalName;
                baseMetadata["root_namespace"] = root.Name.NamespaceName;

                // Extract attributes
                foreach (var attr in root.Attributes())
                {
                    baseMetadata[$"attr_{attr.Name.LocalName}"] = attr.Value;
                }
            }

            if (SplitByElement)
            {
                // Split by specified elements or all root-level elements
                var elementsToProcess = SplitElementNames != null && SplitElementNames.Length > 0
                    ? root.Elements().Where(e => SplitElementNames.Contains(e.Name.LocalName))
                    : root.Elements();

                var index = 0;
                foreach (var element in elementsToProcess)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var content = ExtractContent(element);
                    if (string.IsNullOrWhiteSpace(content))
                        continue;

                    var metadata = new Dictionary<string, object>(baseMetadata)
                    {
                        ["element_name"] = element.Name.LocalName,
                        ["element_namespace"] = element.Name.NamespaceName,
                        ["element_index"] = index
                    };

                    if (PreserveStructure)
                    {
                        // Add element attributes to metadata
                        foreach (var attr in element.Attributes())
                        {
                            metadata[$"attr_{attr.Name.LocalName}"] = attr.Value;
                        }
                    }

                    documents.Add(new Document
                    {
                        Content = content,
                        Metadata = metadata
                    });

                    index++;
                }
            }
            else
            {
                // Single document with all content
                var content = ExtractContent(root);
                documents.Add(new Document
                {
                    Content = content,
                    Metadata = baseMetadata
                });
            }
        }

        return documents;
    }

    private string ExtractContent(XElement element)
    {
        if (ExtractTextOnly)
        {
            // Extract text content, removing XML tags
            return element.Value.Trim();
        }

        // Return XML content with tags
        return element.ToString();
    }
}
