using OfficeOpenXml;
using System.Text;

using DotNetAgents.Abstractions.Documents;



/// <summary>
/// Loads Excel documents from file system.
/// </summary>
public class ExcelDocumentLoader : IDocumentLoader
{
    /// <summary>
    /// Gets or sets whether to split by worksheet. Default is true (one document per worksheet).
    /// </summary>
    public bool SplitByWorksheet { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to split by row. Default is false (all rows in one document per worksheet).
    /// </summary>
    public bool SplitByRow { get; set; } = false;

    /// <summary>
    /// Gets or sets whether the first row contains headers. Default is true.
    /// </summary>
    public bool HasHeaders { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelDocumentLoader"/> class.
    /// </summary>
    /// <param name="splitByWorksheet">Whether to split by worksheet. Default is true.</param>
    /// <param name="splitByRow">Whether to split by row. Default is false.</param>
    /// <param name="hasHeaders">Whether the first row contains headers. Default is true.</param>
    public ExcelDocumentLoader(bool splitByWorksheet = true, bool splitByRow = false, bool hasHeaders = true)
    {
        SplitByWorksheet = splitByWorksheet;
        SplitByRow = splitByRow;
        HasHeaders = hasHeaders;
    }

    /// <summary>
    /// Loads an Excel document from a file path.
    /// </summary>
    /// <param name="source">The file path to the Excel file.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of documents, one per worksheet (or row if SplitByRow is true).</returns>
    public Task<IReadOnlyList<Document>> LoadAsync(
        string source,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be null or whitespace.", nameof(source));

        if (!File.Exists(source))
            throw new FileNotFoundException($"Excel file not found: {source}", source);

        if (!source.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) &&
            !source.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Source must be an Excel file (.xlsx or .xls).", nameof(source));
        }

        return LoadFromFileAsync(source, cancellationToken);
    }

    private Task<IReadOnlyList<Document>> LoadFromFileAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ExcelPackage.License.SetNonCommercialPersonal("DotNetAgents"); // EPPlus 8+ non-commercial license

        var documents = new List<Document>();
        var fileName = Path.GetFileName(filePath);
        var fileInfo = new FileInfo(filePath);

        using var package = new ExcelPackage(new FileInfo(filePath));
        var worksheets = package.Workbook.Worksheets;

        if (worksheets.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<Document>>(documents);
        }

        foreach (var worksheet in worksheets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (worksheet.Dimension == null)
            {
                continue; // Skip empty worksheets
            }

            var startRow = HasHeaders ? 2 : 1; // Start from row 2 if headers exist
            var headers = HasHeaders ? GetRowValues(worksheet, 1) : null;

            if (SplitByRow)
            {
                // Create one document per row
                for (int row = startRow; row <= worksheet.Dimension.End.Row; row++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var rowValues = GetRowValues(worksheet, row);
                    var content = FormatRow(rowValues, headers);
                    var metadata = new Dictionary<string, object>
                    {
                        ["source"] = filePath,
                        ["filename"] = fileName,
                        ["type"] = "excel",
                        ["worksheet"] = worksheet.Name,
                        ["row_number"] = row,
                        ["column_count"] = rowValues.Count,
                        ["file_size"] = fileInfo.Length,
                        ["created_at"] = fileInfo.CreationTimeUtc
                    };

                    // Add column values as metadata if headers exist
                    if (headers != null && rowValues.Count == headers.Count)
                    {
                        for (int col = 0; col < headers.Count && col < rowValues.Count; col++)
                        {
                            metadata[$"column_{headers[col]}"] = rowValues[col];
                        }
                    }

                    documents.Add(new Document
                    {
                        Content = content,
                        Metadata = metadata
                    });
                }
            }
            else
            {
                // Create one document per worksheet
                var content = new StringBuilder();

                if (HasHeaders && headers != null)
                {
                    content.AppendLine(string.Join(" | ", headers.Select((h, i) => $"Column {i + 1}: {h}")));
                    content.AppendLine();
                }

                for (int row = startRow; row <= worksheet.Dimension.End.Row; row++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var rowValues = GetRowValues(worksheet, row);
                    content.AppendLine(FormatRow(rowValues, headers));
                }

                documents.Add(new Document
                {
                    Content = content.ToString().TrimEnd(),
                    Metadata = new Dictionary<string, object>
                    {
                        ["source"] = filePath,
                        ["filename"] = fileName,
                        ["type"] = "excel",
                        ["worksheet"] = worksheet.Name,
                        ["row_count"] = worksheet.Dimension.End.Row - startRow + 1,
                        ["column_count"] = worksheet.Dimension.End.Column,
                        ["file_size"] = fileInfo.Length,
                        ["created_at"] = fileInfo.CreationTimeUtc
                    }
                });
            }
        }

        return Task.FromResult<IReadOnlyList<Document>>(documents);
    }

    private static List<string> GetRowValues(ExcelWorksheet worksheet, int row)
    {
        var values = new List<string>();
        if (worksheet.Dimension == null)
            return values;

        for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
        {
            var cell = worksheet.Cells[row, col];
            var value = cell.Value?.ToString() ?? string.Empty;
            values.Add(value);
        }

        return values;
    }

    private static string FormatRow(List<string> values, List<string>? headers)
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

        // Format as numbered columns
        var numbered = new List<string>();
        for (int i = 0; i < values.Count; i++)
        {
            numbered.Add($"Column {i + 1}: {values[i]}");
        }
        return string.Join(" | ", numbered);
    }
}
