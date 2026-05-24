// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.Chains;

namespace DotNetAgents.Core.Chains;

/// <summary>
/// Base implementation of <see cref="IRunnable{TInput, TOutput}"/> that wraps a function.
/// </summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TOutput">The output type.</typeparam>
public class Runnable<TInput, TOutput> : IRunnable<TInput, TOutput>
{
    private readonly Func<TInput, CancellationToken, Task<TOutput>> _func;

    /// <summary>
    /// Initializes a new instance of the <see cref="Runnable{TInput, TOutput}"/> class.
    /// </summary>
    /// <param name="func">The function to execute.</param>
    /// <exception cref="ArgumentNullException">Thrown when func is null.</exception>
    public Runnable(Func<TInput, CancellationToken, Task<TOutput>> func)
    {
        _func = func ?? throw new ArgumentNullException(nameof(func));
    }

    /// <inheritdoc/>
    public Task<TOutput> InvokeAsync(
        TInput input,
        DotNetAgents.Abstractions.Chains.RunnableOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = ChainTracing.StartActivity("chain.runnable.invoke", "runnable", options);
        return _func(input, cancellationToken);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<TOutput> StreamAsync(
        TInput input,
        DotNetAgents.Abstractions.Chains.RunnableOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var result = await InvokeAsync(input, options, cancellationToken).ConfigureAwait(false);
        yield return result;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TOutput>> BatchAsync(
        IEnumerable<TInput> inputs,
        DotNetAgents.Abstractions.Chains.RunnableOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (inputs == null)
            throw new ArgumentNullException(nameof(inputs));

        var results = new List<TOutput>();
        foreach (var input in inputs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await InvokeAsync(input, options, cancellationToken).ConfigureAwait(false);
            results.Add(result);
        }

        return results;
    }
}
