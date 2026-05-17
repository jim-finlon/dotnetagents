using DotNetAgents.Abstractions.Models;

namespace DotNetAgents.Abstractions.Models;

/// <summary>
/// Interface for Large Language Model providers.
/// </summary>
/// <typeparam name="TInput">The type of input expected by the model.</typeparam>
/// <typeparam name="TOutput">The type of output produced by the model.</typeparam>
public interface ILLMModel<TInput, TOutput>
{
    /// <summary>
    /// Generates a response using the LLM model.
    /// </summary>
    /// <param name="input">The input to process.</param>
    /// <param name="options">Optional configuration for the generation.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The generated output.</returns>
    Task<TOutput> GenerateAsync(
        TInput input,
        LLMOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a streaming response using the LLM model.
    /// </summary>
    /// <param name="input">The input to process.</param>
    /// <param name="options">Optional configuration for the generation.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>An async enumerable of output chunks.</returns>
    IAsyncEnumerable<TOutput> GenerateStreamAsync(
        TInput input,
        LLMOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates responses for multiple inputs in batch.
    /// </summary>
    /// <param name="inputs">The inputs to process.</param>
    /// <param name="options">Optional configuration for the generation.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of generated outputs.</returns>
    Task<IReadOnlyList<TOutput>> GenerateBatchAsync(
        IEnumerable<TInput> inputs,
        LLMOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the name of the model.
    /// </summary>
    string ModelName { get; }

    /// <summary>
    /// Gets the maximum number of tokens supported by the model.
    /// </summary>
    int MaxTokens { get; }
}
