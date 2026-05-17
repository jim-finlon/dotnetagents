using DotNetAgents.Abstractions.Chains;

namespace DotNetAgents.Core.Chains;

/// <summary>
/// Represents a chain expression that can be evaluated to produce a runnable chain.
/// Provides LCEL-like declarative syntax for chain composition.
/// </summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TOutput">The output type.</typeparam>
public class ChainExpression<TInput, TOutput>
{
    private readonly IRunnable<TInput, TOutput> _runnable;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainExpression{TInput, TOutput}"/> class.
    /// </summary>
    /// <param name="runnable">The underlying runnable.</param>
    public ChainExpression(IRunnable<TInput, TOutput> runnable)
    {
        _runnable = runnable ?? throw new ArgumentNullException(nameof(runnable));
    }

    /// <summary>
    /// Gets the underlying runnable.
    /// </summary>
    public IRunnable<TInput, TOutput> Runnable => _runnable;

    /// <summary>
    /// Creates a chain expression from a runnable.
    /// </summary>
    /// <param name="runnable">The runnable to wrap.</param>
    /// <returns>A chain expression.</returns>
    public static ChainExpression<TInput, TOutput> From(IRunnable<TInput, TOutput> runnable)
    {
        return new ChainExpression<TInput, TOutput>(runnable);
    }
}

/// <summary>
/// Extension methods for chain expression composition (LCEL-like syntax).
/// </summary>
public static class ChainExpressionExtensions
{
    /// <summary>
    /// Pipe operator for sequential composition (output of first becomes input of second).
    /// </summary>
    /// <typeparam name="TInput">The input type of the first expression.</typeparam>
    /// <typeparam name="TMiddle">The output type of the first expression and input type of the second.</typeparam>
    /// <typeparam name="TFinalOutput">The output type of the second expression.</typeparam>
    /// <param name="first">The first chain expression.</param>
    /// <param name="second">The second chain expression.</param>
    /// <returns>A new chain expression that composes both sequentially.</returns>
    public static ChainExpression<TInput, TFinalOutput> Pipe<TInput, TMiddle, TFinalOutput>(
        this ChainExpression<TInput, TMiddle> first,
        ChainExpression<TMiddle, TFinalOutput> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return new ChainExpression<TInput, TFinalOutput>(first.Runnable.Pipe(second.Runnable));
    }

    /// <summary>
    /// Parallel composition operator (executes both expressions in parallel and combines results).
    /// </summary>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <typeparam name="TOutput1">The output type of the first expression.</typeparam>
    /// <typeparam name="TOutput2">The output type of the second expression.</typeparam>
    /// <param name="first">The first chain expression.</param>
    /// <param name="second">The second chain expression.</param>
    /// <returns>A new chain expression that executes both in parallel.</returns>
    public static ChainExpression<TInput, (TOutput1, TOutput2)> Parallel<TInput, TOutput1, TOutput2>(
        this ChainExpression<TInput, TOutput1> first,
        ChainExpression<TInput, TOutput2> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return new ChainExpression<TInput, (TOutput1, TOutput2)>(
            new ParallelRunnable<TInput, TOutput1, TOutput2>(first.Runnable, second.Runnable));
    }

    /// <summary>
    /// Batch operator for processing multiple inputs.
    /// </summary>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <typeparam name="TOutput">The output type.</typeparam>
    /// <param name="expression">The chain expression.</param>
    /// <param name="inputs">The inputs to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes with the batch results.</returns>
    public static Task<IReadOnlyList<TOutput>> Batch<TInput, TOutput>(
        this ChainExpression<TInput, TOutput> expression,
        IEnumerable<TInput> inputs,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(inputs);

        return expression.Runnable.BatchAsync(inputs, cancellationToken: cancellationToken);
    }

    private sealed class ParallelRunnable<TInput, TOutput1, TOutput2> : IRunnable<TInput, (TOutput1, TOutput2)>
    {
        private readonly IRunnable<TInput, TOutput1> _first;
        private readonly IRunnable<TInput, TOutput2> _second;

        public ParallelRunnable(
            IRunnable<TInput, TOutput1> first,
            IRunnable<TInput, TOutput2> second)
        {
            _first = first;
            _second = second;
        }

        public async Task<(TOutput1, TOutput2)> InvokeAsync(
            TInput input,
            RunnableOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var task1 = _first.InvokeAsync(input, options, cancellationToken);
            var task2 = _second.InvokeAsync(input, options, cancellationToken);

            await Task.WhenAll(task1, task2).ConfigureAwait(false);

            return (await task1.ConfigureAwait(false), await task2.ConfigureAwait(false));
        }

        public async IAsyncEnumerable<(TOutput1, TOutput2)> StreamAsync(
            TInput input,
            RunnableOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var stream1 = _first.StreamAsync(input, options, cancellationToken).GetAsyncEnumerator(cancellationToken);
            var stream2 = _second.StreamAsync(input, options, cancellationToken).GetAsyncEnumerator(cancellationToken);

            try
            {
                var hasMore1 = await stream1.MoveNextAsync().ConfigureAwait(false);
                var hasMore2 = await stream2.MoveNextAsync().ConfigureAwait(false);

                while (hasMore1 || hasMore2)
                {
                    var output1 = hasMore1 ? stream1.Current : default(TOutput1)!;
                    var output2 = hasMore2 ? stream2.Current : default(TOutput2)!;

                    yield return (output1, output2);

                    if (hasMore1)
                        hasMore1 = await stream1.MoveNextAsync().ConfigureAwait(false);
                    if (hasMore2)
                        hasMore2 = await stream2.MoveNextAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                await stream1.DisposeAsync().ConfigureAwait(false);
                await stream2.DisposeAsync().ConfigureAwait(false);
            }
        }

        public async Task<IReadOnlyList<(TOutput1, TOutput2)>> BatchAsync(
            IEnumerable<TInput> inputs,
            RunnableOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var inputList = inputs.ToList();
            var results1 = await _first.BatchAsync(inputList, options, cancellationToken).ConfigureAwait(false);
            var results2 = await _second.BatchAsync(inputList, options, cancellationToken).ConfigureAwait(false);

            var combined = new List<(TOutput1, TOutput2)>();
            for (int i = 0; i < Math.Min(results1.Count, results2.Count); i++)
            {
                combined.Add((results1[i], results2[i]));
            }

            return combined;
        }
    }
}
