namespace DotNetAgents.Knowledge.Export;

/// <summary>
/// Supported export formats for AI fine-tuning.
/// </summary>
public enum KnowledgeExportFormat
{
    /// <summary>
    /// OpenAI fine-tuning format (JSONL with messages array).
    /// </summary>
    OpenAiJsonl,

    /// <summary>
    /// Anthropic Claude fine-tuning format (JSONL with messages array).
    /// </summary>
    AnthropicJsonl,

    /// <summary>
    /// Simple instruction-response format (prompt/completion pairs).
    /// </summary>
    InstructionResponse,

    /// <summary>
    /// ChatML format for compatible models.
    /// </summary>
    ChatML
}
