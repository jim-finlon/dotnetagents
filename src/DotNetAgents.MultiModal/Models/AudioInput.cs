// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.MultiModal.Models;

/// <summary>Input for audio operations (transcription).</summary>
public sealed record AudioInput
{
    /// <summary>Audio file path.</summary>
    public string? FilePath { get; init; }

    /// <summary>Audio bytes.</summary>
    public byte[]? Data { get; init; }

    /// <summary>Optional stream (caller keeps ownership).</summary>
    public Stream? Stream { get; init; }

    /// <summary>MIME type (e.g. audio/mpeg).</summary>
    public string? MimeType { get; init; }

    /// <summary>Optional language hint for transcription.</summary>
    public string? Language { get; init; }
}
