namespace DotNetAgents.Abstractions.Documents;

/// <summary>
/// Represents a document with content and metadata.
/// </summary>
public record Document
{
    /// <summary>
    /// Gets or sets the content of the document.
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets metadata associated with the document.
    /// </summary>
    public IDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets the page number if the document is part of a larger document.
    /// </summary>
    public int? PageNumber { get; init; }
}
