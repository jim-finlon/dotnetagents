namespace DotNetAgents.Abstractions.Models;

/// <summary>
/// Represents options for LLM generation requests.
/// </summary>
public record LLMOptions
{
    /// <summary>
    /// Gets or sets the temperature for generation (0.0 to 2.0).
    /// Higher values make output more random.
    /// </summary>
    public double? Temperature { get; init; }

    /// <summary>
    /// Gets or sets the maximum number of tokens to generate.
    /// </summary>
    public int? MaxTokens { get; init; }

    /// <summary>
    /// Gets or sets the top-p (nucleus) sampling parameter.
    /// </summary>
    public double? TopP { get; init; }

    /// <summary>
    /// Gets or sets the frequency penalty (-2.0 to 2.0).
    /// </summary>
    public double? FrequencyPenalty { get; init; }

    /// <summary>
    /// Gets or sets the presence penalty (-2.0 to 2.0).
    /// </summary>
    public double? PresencePenalty { get; init; }

    /// <summary>
    /// Gets or sets additional provider-specific options.
    /// </summary>
    public IDictionary<string, object>? AdditionalOptions { get; init; }
}
