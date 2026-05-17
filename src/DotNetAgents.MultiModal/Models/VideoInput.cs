namespace DotNetAgents.MultiModal.Models;

/// <summary>Input for video analysis.</summary>
public sealed record VideoInput
{
    /// <summary>Video file path.</summary>
    public string? FilePath { get; init; }

    /// <summary>Video bytes.</summary>
    public byte[]? Data { get; init; }

    /// <summary>Optional stream.</summary>
    public Stream? Stream { get; init; }

    /// <summary>MIME type (e.g. video/mp4).</summary>
    public string? MimeType { get; init; }
}
