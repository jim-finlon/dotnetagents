// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.MultiModal.Models;

/// <summary>Result of video analysis (key frames, scenes, summary).</summary>
public sealed record VideoAnalysis
{
    /// <summary>Short summary of the video.</summary>
    public string? Summary { get; init; }

    /// <summary>Detected scenes (e.g. start/end time and description).</summary>
    public IReadOnlyList<VideoScene>? Scenes { get; init; }

    /// <summary>Transcription of the audio track if requested.</summary>
    public string? Transcription { get; init; }
}

/// <summary>A detected scene in a video.</summary>
public sealed record VideoScene(TimeSpan Start, TimeSpan End, string? Description);
