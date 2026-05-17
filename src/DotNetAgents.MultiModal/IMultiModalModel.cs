using DotNetAgents.Abstractions.Models;
using DotNetAgents.MultiModal.Models;

namespace DotNetAgents.MultiModal;

/// <summary>
/// Multi-modal model contract: chat over mixed content plus vision, transcription, TTS, and video analysis.
/// Extends <see cref="ILLMModel{TInput, TOutput}"/> for <see cref="IMultiModalInput"/> and <see cref="MultiModalOutput"/>.
/// </summary>
public interface IMultiModalModel : ILLMModel<IMultiModalInput, MultiModalOutput>
{
    /// <summary>Describe an image or answer a question about it. When <paramref name="prompt"/> is null, returns a description.</summary>
    Task<string> DescribeImageAsync(ImageInput input, string? prompt, CancellationToken cancellationToken = default);

    /// <summary>Transcribe audio to text.</summary>
    Task<string> TranscribeAsync(AudioInput input, CancellationToken cancellationToken = default);

    /// <summary>Synthesize speech from text.</summary>
    Task<byte[]> SynthesizeSpeechAsync(string text, VoiceOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Analyze video (e.g. key frames, scenes, summary).</summary>
    Task<VideoAnalysis> AnalyzeVideoAsync(VideoInput input, CancellationToken cancellationToken = default);

    /// <summary>Returns which capabilities this model supports.</summary>
    ModelCapabilities GetCapabilities();
}
