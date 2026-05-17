namespace DotNetAgents.Abstractions.Models;

/// <summary>
/// Factory interface for creating LLM model instances.
/// </summary>
public interface ILLMModelFactory
{
    /// <summary>
    /// Creates an LLM model instance for the specified provider and model.
    /// </summary>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <typeparam name="TOutput">The output type.</typeparam>
    /// <param name="providerName">The name of the provider (e.g., "OpenAI", "Azure").</param>
    /// <param name="modelName">The name of the model.</param>
    /// <returns>An LLM model instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the provider or model is not supported.</exception>
    ILLMModel<TInput, TOutput> Create<TInput, TOutput>(string providerName, string modelName);

    /// <summary>
    /// Creates an LLM model instance from configuration.
    /// </summary>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <typeparam name="TOutput">The output type.</typeparam>
    /// <param name="configurationKey">The configuration key to use.</param>
    /// <returns>An LLM model instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the configuration key is not found.</exception>
    ILLMModel<TInput, TOutput> CreateFromConfiguration<TInput, TOutput>(string configurationKey);

    /// <summary>
    /// Gets a list of available provider names.
    /// </summary>
    /// <returns>A list of provider names.</returns>
    IReadOnlyList<string> GetAvailableProviders();
}
