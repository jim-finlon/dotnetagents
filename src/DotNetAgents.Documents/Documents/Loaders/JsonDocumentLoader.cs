using System.Text;
using System.Text.Json;
using DotNetAgents.Abstractions.Documents;

namespace DotNetAgents.Documents.Loaders;

/// <summary>
/// Loads JSON documents from file system or string content.
/// </summary>
public class JsonDocumentLoader : IDocumentLoader
{
    /// <summary>
    /// Gets or sets whether to flatten nested JSON structures.
    /// Default is true.
    /// </summary>
    public bool FlattenNested { get; set; } = true;

    /// <summary>
    /// Gets or sets the separator to use when flattening nested structures.
    /// Default is ".".
    /// </summary>
    public string FlattenSeparator { get; set; } = ".";

    /// <summary>
    /// Gets or sets whether to split JSON arrays into separate documents.
    /// Default is false (single document with all content).
    /// </summary>
    public bool SplitByArrayItem { get; set; } = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDocumentLoader"/> class.
    /// </summary>
    /// <param name="flattenNested">Whether to flatten nested JSON structures. Default is true.</param>
    /// <param name="flattenSeparator">The separator to use when flattening nested structures. Default is ".". </param>
    /// <param name="splitByArrayItem">Whether to split JSON arrays into separate documents. Default is false.</param>
    public JsonDocumentLoader(
        bool flattenNested = true,
        string flattenSeparator = ".",
        bool splitByArrayItem = false)
    {
        FlattenNested = flattenNested;
        FlattenSeparator = flattenSeparator;
        SplitByArrayItem = splitByArrayItem;
    }

    /// <summary>
    /// Loads a JSON document from a file path or string content.
    /// </summary>
    /// <param name="source">The file path or JSON content to load.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of documents.</returns>
    public async Task<IReadOnlyList<Document>> LoadAsync(
        string source,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be null or whitespace.", nameof(source));

        JsonDocument jsonDoc;
        string? filePath = null;

        // Check if source is a file path
        if (File.Exists(source))
        {
            filePath = source;
            var content = await File.ReadAllTextAsync(source, cancellationToken).ConfigureAwait(false);
            jsonDoc = JsonDocument.Parse(content);
        }
        else
        {
            // Treat as raw JSON content
            jsonDoc = JsonDocument.Parse(source);
        }

        return ProcessJsonDocument(jsonDoc, filePath, cancellationToken);
    }

    private IReadOnlyList<Document> ProcessJsonDocument(
        JsonDocument jsonDoc,
        string? filePath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var documents = new List<Document>();
        var baseMetadata = new Dictionary<string, object>
        {
            ["type"] = "json"
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

        var rootElement = jsonDoc.RootElement;

        if (SplitByArrayItem && rootElement.ValueKind == JsonValueKind.Array)
        {
            // Split array into separate documents
            var index = 0;
            foreach (var item in rootElement.EnumerateArray())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var content = FormatJsonElement(item);
                var metadata = new Dictionary<string, object>(baseMetadata)
                {
                    ["array_index"] = index,
                    ["total_items"] = rootElement.GetArrayLength()
                };

                if (FlattenNested)
                {
                    FlattenJsonElement(item, metadata, "");
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
            // Single document
            var content = FormatJsonElement(rootElement);
            var metadata = new Dictionary<string, object>(baseMetadata);

            if (FlattenNested)
            {
                FlattenJsonElement(rootElement, metadata, "");
            }

            documents.Add(new Document
            {
                Content = content,
                Metadata = metadata
            });
        }

        return documents;
    }

    private static string FormatJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => FormatJsonObject(element),
            JsonValueKind.Array => FormatJsonArray(element),
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            _ => element.GetRawText()
        };
    }

    private static string FormatJsonObject(JsonElement obj)
    {
        var builder = new StringBuilder();
        foreach (var property in obj.EnumerateObject())
        {
            var value = FormatJsonElement(property.Value);
            builder.AppendLine($"{property.Name}: {value}");
        }
        return builder.ToString().Trim();
    }

    private static string FormatJsonArray(JsonElement array)
    {
        var builder = new StringBuilder();
        var index = 0;
        foreach (var item in array.EnumerateArray())
        {
            var value = FormatJsonElement(item);
            builder.AppendLine($"[{index}]: {value}");
            index++;
        }
        return builder.ToString().Trim();
    }

    private void FlattenJsonElement(JsonElement element, Dictionary<string, object> metadata, string prefix)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix)
                        ? property.Name
                        : $"{prefix}{FlattenSeparator}{property.Name}";
                    FlattenJsonElement(property.Value, metadata, key);
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var key = string.IsNullOrEmpty(prefix)
                        ? $"[{index}]"
                        : $"{prefix}{FlattenSeparator}[{index}]";
                    FlattenJsonElement(item, metadata, key);
                    index++;
                }
                break;

            case JsonValueKind.String:
                metadata[prefix] = element.GetString() ?? string.Empty;
                break;

            case JsonValueKind.Number:
                if (element.TryGetInt64(out var intValue))
                    metadata[prefix] = intValue;
                else if (element.TryGetDouble(out var doubleValue))
                    metadata[prefix] = doubleValue;
                else
                    metadata[prefix] = element.GetRawText();
                break;

            case JsonValueKind.True:
                metadata[prefix] = true;
                break;

            case JsonValueKind.False:
                metadata[prefix] = false;
                break;

            case JsonValueKind.Null:
                metadata[prefix] = null!;
                break;
        }
    }
}
