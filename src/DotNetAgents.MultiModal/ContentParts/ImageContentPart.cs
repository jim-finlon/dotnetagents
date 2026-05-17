namespace DotNetAgents.MultiModal.ContentParts;

/// <summary>Content part holding image data (base64) or a URL.</summary>
public sealed class ImageContentPart : IContentPart
{
    /// <inheritdoc />
    public ContentType Type => ContentType.Image;

    /// <inheritdoc />
    public object Content { get; }

    /// <summary>Optional MIME type (e.g. image/jpeg, image/png).</summary>
    public string? MimeType { get; }

    /// <summary>Creates an image part from base64 data.</summary>
    public ImageContentPart(byte[] imageData, string? mimeType = null)
    {
        ArgumentNullException.ThrowIfNull(imageData);
        Content = imageData;
        MimeType = mimeType ?? "image/jpeg";
    }

    /// <summary>Creates an image part from a URL.</summary>
    public ImageContentPart(Uri imageUrl)
    {
        ArgumentNullException.ThrowIfNull(imageUrl);
        Content = imageUrl;
    }
}
