// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.MultiModal.ContentParts;

/// <summary>Content part holding video data (path or bytes).</summary>
public sealed class VideoContentPart : IContentPart
{
    /// <inheritdoc />
    public ContentType Type => ContentType.Video;

    /// <inheritdoc />
    public object Content { get; }

    /// <summary>Optional MIME type (e.g. video/mp4).</summary>
    public string? MimeType { get; }

    /// <summary>Creates a video part from a file path.</summary>
    public VideoContentPart(string filePath, string? mimeType = null)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        Content = filePath;
        MimeType = mimeType ?? "video/mp4";
    }

    /// <summary>Creates a video part from bytes.</summary>
    public VideoContentPart(byte[] videoData, string? mimeType = null)
    {
        ArgumentNullException.ThrowIfNull(videoData);
        Content = videoData;
        MimeType = mimeType ?? "video/mp4";
    }
}
