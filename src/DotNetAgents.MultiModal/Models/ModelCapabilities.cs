// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.MultiModal.Models;

/// <summary>Capabilities supported by a multi-modal model.</summary>
[Flags]
public enum ModelCapabilities
{
    /// <summary>None.</summary>
    None = 0,

    /// <summary>Text generation (chat/completion).</summary>
    Text = 1,

    /// <summary>Vision (image understanding).</summary>
    Vision = 2,

    /// <summary>Speech-to-text (transcription).</summary>
    Transcription = 4,

    /// <summary>Text-to-speech.</summary>
    SpeechSynthesis = 8,

    /// <summary>Video analysis.</summary>
    Video = 16,

    /// <summary>Tool/function calling.</summary>
    ToolUse = 32
}
