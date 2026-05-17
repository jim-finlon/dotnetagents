using DotNetAgents.Abstractions.Chains;

namespace DotNetAgents.Abstractions.Chains;

/// <summary>
/// Core interface for runnable components that can be composed into chains.
/// </summary>
/// <typeparam name="TInput">The type of input.</typeparam>
/// <typeparam name="TOutput">The type of output.</typeparam>
public interface IRunnable<TInput, TOutput>
{
    /// <summary>
    /// Invokes the runnable with the given input.
    /// </summary>
    /// <param name="input">The input to process.</param>
    /// <param name="options">Optional configuration for the execution.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The output result.</returns>
    Task<TOutput> InvokeAsync(
        TInput input,
        RunnableOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams the output of the runnable.
    /// </summary>
    /// <param name="input">The input to process.</param>
    /// <param name="options">Optional configuration for the execution.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>An async enumerable of output chunks.</returns>
    IAsyncEnumerable<TOutput> StreamAsync(
        TInput input,
        RunnableOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes multiple inputs in batch.
    /// </summary>
    /// <param name="inputs">The inputs to process.</param>
    /// <param name="options">Optional configuration for the execution.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of outputs.</returns>
    Task<IReadOnlyList<TOutput>> BatchAsync(
        IEnumerable<TInput> inputs,
        RunnableOptions? options = null,
        CancellationToken cancellationToken = default);
}
