namespace DotNetAgents.MultiModal.Models;

/// <summary>Options for text-to-speech synthesis.</summary>
public sealed record VoiceOptions
{
    /// <summary>Voice identifier (provider-specific).</summary>
    public string? VoiceId { get; init; }

    /// <summary>Speaking rate (e.g. 1.0 = normal).</summary>
    public double? Speed { get; init; }

    /// <summary>Language code (e.g. en-US).</summary>
    public string? Language { get; init; }
}
