namespace DotNetAgents.MultiModal.Models;

/// <summary>Input for vision operations (describe image, QA, OCR).</summary>
public sealed record ImageInput
{
    /// <summary>Image as base64-encoded bytes.</summary>
    public byte[]? ImageDataBase64 { get; init; }

    /// <summary>Image URL.</summary>
    public Uri? ImageUrl { get; init; }

    /// <summary>MIME type (e.g. image/jpeg).</summary>
    public string? MimeType { get; init; }
}
