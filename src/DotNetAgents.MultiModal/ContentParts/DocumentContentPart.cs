namespace DotNetAgents.MultiModal.ContentParts;

/// <summary>Content part holding a document (e.g. PDF) path or bytes.</summary>
public sealed class DocumentContentPart : IContentPart
{
    /// <inheritdoc />
    public ContentType Type => ContentType.Document;

    /// <inheritdoc />
    public object Content { get; }

    /// <summary>Optional MIME type (e.g. application/pdf).</summary>
    public string? MimeType { get; }

    /// <summary>Creates a document part from a file path.</summary>
    public DocumentContentPart(string filePath, string? mimeType = null)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        Content = filePath;
        MimeType = mimeType ?? "application/pdf";
    }

    /// <summary>Creates a document part from bytes.</summary>
    public DocumentContentPart(byte[] data, string? mimeType = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        Content = data;
        MimeType = mimeType ?? "application/pdf";
    }
}
