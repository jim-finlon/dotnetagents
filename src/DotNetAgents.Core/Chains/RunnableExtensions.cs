using DotNetAgents.Abstractions.Chains;

namespace DotNetAgents.Core.Chains;

/// <summary>
/// Extension methods for <see cref="IRunnable{TInput, TOutput}"/> composition.
/// </summary>
public static class RunnableExtensions
{
    /// <summary>
    /// Composes two runnables sequentially (pipes output of first to input of second).
    /// </summary>
    /// <typeparam name="TInput">The input type of the first runnable.</typeparam>
    /// <typeparam name="TMiddle">The output type of the first runnable and input type of the second.</typeparam>
    /// <typeparam name="TFinalOutput">The output type of the second runnable.</typeparam>
    /// <param name="first">The first runnable to execute.</param>
    /// <param name="second">The second runnable to execute.</param>
    /// <returns>A new runnable that composes both runnables.</returns>
    public static IRunnable<TInput, TFinalOutput> Pipe<TInput, TMiddle, TFinalOutput>(
        this IRunnable<TInput, TMiddle> first,
        IRunnable<TMiddle, TFinalOutput> second)
    {
        if (first == null)
            throw new ArgumentNullException(nameof(first));
        if (second == null)
            throw new ArgumentNullException(nameof(second));

        return new PipeRunnable<TInput, TMiddle, TFinalOutput>(first, second);
    }

    /// <summary>
    /// Applies a transformation function to the output of a runnable.
    /// </summary>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <typeparam name="TOutput">The output type.</typeparam>
    /// <param name="runnable">The runnable to wrap.</param>
    /// <param name="mapper">The transformation function to apply.</param>
    /// <returns>A new runnable that applies the transformation.</returns>
    public static IRunnable<TInput, TOutput> Map<TInput, TOutput>(
        this IRunnable<TInput, TOutput> runnable,
        Func<TOutput, TOutput> mapper)
    {
        if (runnable == null)
            throw new ArgumentNullException(nameof(runnable));
        if (mapper == null)
            throw new ArgumentNullException(nameof(mapper));

        return new MapRunnable<TInput, TOutput>(runnable, mapper);
    }

    private sealed class PipeRunnable<TInput, TMiddle, TFinalOutput> : IRunnable<TInput, TFinalOutput>
    {
        private readonly IRunnable<TInput, TMiddle> _first;
        private readonly IRunnable<TMiddle, TFinalOutput> _second;

        public PipeRunnable(IRunnable<TInput, TMiddle> first, IRunnable<TMiddle, TFinalOutput> second)
        {
            _first = first;
            _second = second;
        }

        public async Task<TFinalOutput> InvokeAsync(
            TInput input,
            DotNetAgents.Abstractions.Chains.RunnableOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var middle = await _first.InvokeAsync(input, options, cancellationToken).ConfigureAwait(false);
            return await _second.InvokeAsync(middle, options, cancellationToken).ConfigureAwait(false);
        }

        public async IAsyncEnumerable<TFinalOutput> StreamAsync(
            TInput input,
            DotNetAgents.Abstractions.Chains.RunnableOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var middle in _first.StreamAsync(input, options, cancellationToken).ConfigureAwait(false))
            {
                await foreach (var output in _second.StreamAsync(middle, options, cancellationToken).ConfigureAwait(false))
                {
                    yield return output;
                }
            }
        }

        public async Task<IReadOnlyList<TFinalOutput>> BatchAsync(
            IEnumerable<TInput> inputs,
            DotNetAgents.Abstractions.Chains.RunnableOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var middleResults = await _first.BatchAsync(inputs, options, cancellationToken).ConfigureAwait(false);
            return await _second.BatchAsync(middleResults, options, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class MapRunnable<TInput, TOutput> : IRunnable<TInput, TOutput>
    {
        private readonly IRunnable<TInput, TOutput> _runnable;
        private readonly Func<TOutput, TOutput> _mapper;

        public MapRunnable(IRunnable<TInput, TOutput> runnable, Func<TOutput, TOutput> mapper)
        {
            _runnable = runnable;
            _mapper = mapper;
        }

        public async Task<TOutput> InvokeAsync(
            TInput input,
            DotNetAgents.Abstractions.Chains.RunnableOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _runnable.InvokeAsync(input, options, cancellationToken).ConfigureAwait(false);
            return _mapper(result);
        }

        public async IAsyncEnumerable<TOutput> StreamAsync(
            TInput input,
            DotNetAgents.Abstractions.Chains.RunnableOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var item in _runnable.StreamAsync(input, options, cancellationToken).ConfigureAwait(false))
            {
                yield return _mapper(item);
            }
        }

        public async Task<IReadOnlyList<TOutput>> BatchAsync(
            IEnumerable<TInput> inputs,
            DotNetAgents.Abstractions.Chains.RunnableOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var results = await _runnable.BatchAsync(inputs, options, cancellationToken).ConfigureAwait(false);
            return results.Select(_mapper).ToList();
        }
    }
}
