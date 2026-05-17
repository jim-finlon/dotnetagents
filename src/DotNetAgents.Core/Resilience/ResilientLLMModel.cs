using DotNetAgents.Abstractions.Models;
using DotNetAgents.Abstractions.Resilience;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Core.Resilience;

/// <summary>
/// Wraps an LLM model with retry logic and circuit breaker protection.
/// </summary>
/// <typeparam name="TInput">The type of input expected by the model.</typeparam>
/// <typeparam name="TOutput">The type of output produced by the model.</typeparam>
public class ResilientLLMModel<TInput, TOutput> : ILLMModel<TInput, TOutput>
{
    private readonly ILLMModel<TInput, TOutput> _innerModel;
    private readonly RetryPolicy? _retryPolicy;
    private readonly CircuitBreaker? _circuitBreaker;
    private readonly ILogger<ResilientLLMModel<TInput, TOutput>>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilientLLMModel{TInput, TOutput}"/> class.
    /// </summary>
    /// <param name="innerModel">The underlying LLM model to wrap.</param>
    /// <param name="retryPolicy">Optional retry policy.</param>
    /// <param name="circuitBreaker">Optional circuit breaker.</param>
    /// <param name="logger">Optional logger for resilient operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when innerModel is null.</exception>
    public ResilientLLMModel(
        ILLMModel<TInput, TOutput> innerModel,
        RetryPolicy? retryPolicy = null,
        CircuitBreaker? circuitBreaker = null,
        ILogger<ResilientLLMModel<TInput, TOutput>>? logger = null)
    {
        _innerModel = innerModel ?? throw new ArgumentNullException(nameof(innerModel));
        _retryPolicy = retryPolicy;
        _circuitBreaker = circuitBreaker;
        _logger = logger;
    }

    /// <inheritdoc/>
    public string ModelName => _innerModel.ModelName;

    /// <inheritdoc/>
    public int MaxTokens => _innerModel.MaxTokens;

    /// <inheritdoc/>
    public async Task<TOutput> GenerateAsync(
        TInput input,
        DotNetAgents.Abstractions.Models.LLMOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Func<CancellationToken, Task<TOutput>> operation = async ct =>
        {
            return await _innerModel.GenerateAsync(input, options, ct).ConfigureAwait(false);
        };

        if (_circuitBreaker != null && _retryPolicy != null)
        {
            // Apply circuit breaker, then retry policy
            return await _retryPolicy.ExecuteAsync(
                async ct => await _circuitBreaker.ExecuteAsync(operation, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }
        else if (_circuitBreaker != null)
        {
            return await _circuitBreaker.ExecuteAsync(operation, cancellationToken).ConfigureAwait(false);
        }
        else if (_retryPolicy != null)
        {
            return await _retryPolicy.ExecuteAsync(operation, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            return await operation(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<TOutput> GenerateStreamAsync(
        TInput input,
        DotNetAgents.Abstractions.Models.LLMOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // For streaming, we apply resilience at the operation level
        // Note: Circuit breaker and retry logic for streaming is more complex
        // For now, we'll just wrap the inner stream
        await foreach (var item in _innerModel.GenerateStreamAsync(input, options, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TOutput>> GenerateBatchAsync(
        IEnumerable<TInput> inputs,
        DotNetAgents.Abstractions.Models.LLMOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Func<CancellationToken, Task<IReadOnlyList<TOutput>>> operation = async ct =>
        {
            return await _innerModel.GenerateBatchAsync(inputs, options, ct).ConfigureAwait(false);
        };

        if (_circuitBreaker != null && _retryPolicy != null)
        {
            return await _retryPolicy.ExecuteAsync(
                async ct => await _circuitBreaker.ExecuteAsync(operation, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }
        else if (_circuitBreaker != null)
        {
            return await _circuitBreaker.ExecuteAsync(operation, cancellationToken).ConfigureAwait(false);
        }
        else if (_retryPolicy != null)
        {
            return await _retryPolicy.ExecuteAsync(operation, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            return await operation(cancellationToken).ConfigureAwait(false);
        }
    }
}
