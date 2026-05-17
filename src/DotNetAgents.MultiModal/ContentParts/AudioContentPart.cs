namespace DotNetAgents.MultiModal.ContentParts;

/// <summary>Content part holding audio data (stream/path) or bytes.</summary>
public sealed class AudioContentPart : IContentPart
{
    /// <inheritdoc />
    public ContentType Type => ContentType.Audio;

    /// <inheritdoc />
    public object Content { get; }

    /// <summary>Optional MIME type (e.g. audio/mpeg, audio/wav).</summary>
    public string? MimeType { get; }

    /// <summary>Creates an audio part from bytes.</summary>
    public AudioContentPart(byte[] audioData, string? mimeType = null)
    {
        ArgumentNullException.ThrowIfNull(audioData);
        Content = audioData;
        MimeType = mimeType;
    }

    /// <summary>Creates an audio part from a file path.</summary>
    public AudioContentPart(string filePath, string? mimeType = null)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        Content = filePath;
        MimeType = mimeType;
    }
}
