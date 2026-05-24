// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using System.Text;

using DotNetAgents.Abstractions.Documents;



/// <summary>
/// Loads CSV documents from file system or string content.
/// </summary>
public class CsvDocumentLoader : IDocumentLoader
{
    /// <summary>
    /// Gets or sets the delimiter character. Default is comma (,).
    /// </summary>
    public char Delimiter { get; set; } = ',';

    /// <summary>
    /// Gets or sets whether the first row contains headers. Default is true.
    /// </summary>
    public bool HasHeaders { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include headers in the document content. Default is true.
    /// </summary>
    public bool IncludeHeaders { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvDocumentLoader"/> class.
    /// </summary>
    /// <param name="delimiter">The delimiter character. Default is comma (,).</param>
    /// <param name="hasHeaders">Whether the first row contains headers. Default is true.</param>
    /// <param name="includeHeaders">Whether to include headers in the document content. Default is true.</param>
    public CsvDocumentLoader(char delimiter = ',', bool hasHeaders = true, bool includeHeaders = true)
    {
        Delimiter = delimiter;
        HasHeaders = hasHeaders;
        IncludeHeaders = includeHeaders;
    }

    /// <summary>
    /// Loads a CSV document from a file path or string content.
    /// </summary>
    /// <param name="source">The file path or CSV content to load.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list containing the loaded document(s).</returns>
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

        // Treat as raw CSV content
        return await LoadFromContentAsync(source, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<Document>> LoadFromFileAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
        var fileName = Path.GetFileName(filePath);
        var fileInfo = new FileInfo(filePath);

        var documents = await LoadFromContentAsync(content, cancellationToken).ConfigureAwait(false);

        // Add file metadata to all documents
        var updatedDocuments = new List<Document>();
        foreach (var doc in documents)
        {
            var updatedMetadata = new Dictionary<string, object>(doc.Metadata)
            {
                ["source"] = filePath,
                ["filename"] = fileName,
                ["file_size"] = fileInfo.Length,
                ["created_at"] = fileInfo.CreationTimeUtc
            };

            updatedDocuments.Add(doc with { Metadata = updatedMetadata });
        }

        return updatedDocuments;
    }

    private Task<IReadOnlyList<Document>> LoadFromContentAsync(
        string csvContent,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        if (lines.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<Document>>(new[] { new Document { Content = string.Empty, Metadata = new Dictionary<string, object> { ["type"] = "csv" } } });
        }

        var documents = new List<Document>();
        var headers = HasHeaders && lines.Count > 0 ? ParseLine(lines[0]) : null;
        var startIndex = HasHeaders ? 1 : 0;

        if (HasHeaders && IncludeHeaders && headers != null)
        {
            // Create a document with headers
            var headerContent = string.Join(Delimiter.ToString(), headers);
            documents.Add(new Document
            {
                Content = headerContent,
                Metadata = new Dictionary<string, object>
                {
                    ["type"] = "csv",
                    ["row_type"] = "header",
                    ["column_count"] = headers.Count
                }
            });
        }

        // Parse each row
        for (int i = startIndex; i < lines.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var values = ParseLine(lines[i]);
            var rowContent = FormatRow(values, headers);
            var rowMetadata = new Dictionary<string, object>
            {
                ["type"] = "csv",
                ["row_type"] = "data",
                ["row_number"] = i - startIndex + 1,
                ["column_count"] = values.Count
            };

            // Add column values as metadata if headers exist
            if (headers != null && values.Count == headers.Count)
            {
                for (int j = 0; j < headers.Count && j < values.Count; j++)
                {
                    rowMetadata[$"column_{headers[j]}"] = values[j];
                }
            }

            documents.Add(new Document
            {
                Content = rowContent,
                Metadata = rowMetadata
            });
        }

        return Task.FromResult<IReadOnlyList<Document>>(documents);
    }

    private List<string> ParseLine(string line)
    {
        var values = new List<string>();
        var currentValue = new StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Escaped quote
                    currentValue.Append('"');
                    i++; // Skip next quote
                }
                else
                {
                    // Toggle quote state
                    inQuotes = !inQuotes;
                }
            }
            else if (c == Delimiter && !inQuotes)
            {
                // End of field
                values.Add(currentValue.ToString().Trim());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(c);
            }
        }

        // Add last field
        values.Add(currentValue.ToString().Trim());

        return values;
    }

    private string FormatRow(List<string> values, List<string>? headers)
    {
        if (headers != null && values.Count == headers.Count)
        {
            // Format as key-value pairs
            var pairs = new List<string>();
            for (int i = 0; i < headers.Count && i < values.Count; i++)
            {
                pairs.Add($"{headers[i]}: {values[i]}");
            }
            return string.Join(" | ", pairs);
        }

        // Format as comma-separated values
        return string.Join(Delimiter.ToString(), values);
    }
}
