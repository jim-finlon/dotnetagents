namespace DotNetAgents.MultiModal;

/// <summary>
/// Type of content in a multi-modal request or response.
/// </summary>
public enum ContentType
{
    /// <summary>Plain text.</summary>
    Text,

    /// <summary>Image (e.g. JPEG, PNG, GIF, WebP).</summary>
    Image,

    /// <summary>Audio (e.g. MP3, WAV, FLAC, OGG).</summary>
    Audio,

    /// <summary>Video.</summary>
    Video,

    /// <summary>Document (e.g. PDF).</summary>
    Document,

    /// <summary>Generic file reference.</summary>
    File
}
