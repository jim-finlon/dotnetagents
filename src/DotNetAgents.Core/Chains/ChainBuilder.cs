// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.Caching;
using DotNetAgents.Abstractions.Chains;
using DotNetAgents.Abstractions.Memory;
using DotNetAgents.Abstractions.Models;
using DotNetAgents.Abstractions.Prompts;
using DotNetAgents.Abstractions.Exceptions;
using DotNetAgents.Core.Prompts;

namespace DotNetAgents.Core.Chains;

/// <summary>
/// Fluent builder for creating chains.
/// </summary>
/// <typeparam name="TInput">The type of the input.</typeparam>
/// <typeparam name="TOutput">The type of the output.</typeparam>
public class ChainBuilder<TInput, TOutput> where TOutput : class
{
    private ILLMModel<TInput, TOutput>? _llmModel;
    private IPromptTemplate? _promptTemplate;
    private IMemory? _memory;
    private ILLMResponseCache<TInput, TOutput>? _responseCache;
    private int _maxRetries;
    private TimeSpan? _retryDelay;

    /// <summary>
    /// Creates a new chain builder.
    /// </summary>
    /// <returns>A new chain builder instance.</returns>
    public static ChainBuilder<TInput, TOutput> Create()
    {
        return new ChainBuilder<TInput, TOutput>();
    }

    /// <summary>
    /// Configures the LLM model for the chain.
    /// </summary>
    /// <param name="model">The LLM model to use.</param>
    /// <returns>The chain builder for method chaining.</returns>
    public ChainBuilder<TInput, TOutput> WithLLM(ILLMModel<TInput, TOutput> model)
    {
        _llmModel = model ?? throw new ArgumentNullException(nameof(model));
        return this;
    }

    /// <summary>
    /// Configures a prompt template for the chain.
    /// </summary>
    /// <param name="template">The prompt template to use.</param>
    /// <returns>The chain builder for method chaining.</returns>
    public ChainBuilder<TInput, TOutput> WithPromptTemplate(IPromptTemplate template)
    {
        _promptTemplate = template ?? throw new ArgumentNullException(nameof(template));
        return this;
    }

    /// <summary>
    /// Configures a prompt template from a template string.
    /// </summary>
    /// <param name="templateString">The template string.</param>
    /// <returns>The chain builder for method chaining.</returns>
    public ChainBuilder<TInput, TOutput> WithPromptTemplate(string templateString)
    {
        if (string.IsNullOrWhiteSpace(templateString))
            throw new ArgumentException("Template string cannot be null or whitespace.", nameof(templateString));

        _promptTemplate = new PromptTemplate(templateString);
        return this;
    }

    /// <summary>
    /// Configures memory for the chain.
    /// </summary>
    /// <param name="memory">The memory store to use.</param>
    /// <returns>The chain builder for method chaining.</returns>
    public ChainBuilder<TInput, TOutput> WithMemory(IMemory memory)
    {
        _memory = memory ?? throw new ArgumentNullException(nameof(memory));
        return this;
    }

    /// <summary>
    /// Configures response caching for the chain.
    /// </summary>
    /// <param name="cache">The response cache to use.</param>
    /// <returns>The chain builder for method chaining.</returns>
    public ChainBuilder<TInput, TOutput> WithCaching(ILLMResponseCache<TInput, TOutput> cache)
    {
        _responseCache = cache ?? throw new ArgumentNullException(nameof(cache));
        return this;
    }


    /// <summary>
    /// Configures retry policy for the chain.
    /// </summary>
    /// <param name="maxRetries">The maximum number of retries.</param>
    /// <param name="delay">Optional delay between retries.</param>
    /// <returns>The chain builder for method chaining.</returns>
    public ChainBuilder<TInput, TOutput> WithRetryPolicy(int maxRetries, TimeSpan? delay = null)
    {
        if (maxRetries < 0)
            throw new ArgumentException("Max retries cannot be negative.", nameof(maxRetries));

        _maxRetries = maxRetries;
        _retryDelay = delay;
        return this;
    }

    /// <summary>
    /// Builds the chain.
    /// </summary>
    /// <returns>The built chain.</returns>
    /// <exception cref="AgentException">Thrown when required components are missing.</exception>
    public IRunnable<TInput, TOutput> Build()
    {
        if (_llmModel == null)
        {
            throw new AgentException(
                "LLM model is required to build a chain.",
                ErrorCategory.ConfigurationError);
        }

        // Build the chain based on configured components
        IRunnable<TInput, TOutput> chain;

        // If we have both LLM and prompt template, use LLMChain
        if (_promptTemplate != null)
        {
            // LLMChain requires ILLMModel<string, string>, so we need to adapt
            // For now, create a simple wrapper chain
            var llmModel = _llmModel;
            var promptTemplate = _promptTemplate;

            chain = new Runnable<TInput, TOutput>(async (input, ct) =>
            {
                // Convert input to dictionary for template formatting
                var variables = input is IDictionary<string, object> dict
                    ? dict
                    : new Dictionary<string, object> { ["input"] = input! };

                var formattedPrompt = await promptTemplate.FormatAsync(variables, ct).ConfigureAwait(false);

                // Call LLM with formatted prompt
                // Note: This assumes TInput can be converted to string and TOutput is string
                // In a full implementation, we'd need proper type conversion
                var stringInput = formattedPrompt;
                var result = await llmModel.GenerateAsync((TInput)(object)stringInput, cancellationToken: ct).ConfigureAwait(false);
                return result;
            });
        }
        else
        {
            // No prompt template, use LLM directly
            chain = new Runnable<TInput, TOutput>(async (input, ct) =>
            {
                return await _llmModel.GenerateAsync(input, cancellationToken: ct).ConfigureAwait(false);
            });
        }

        // Wrap with rate limiting if configured (via memory or other mechanism)
        // Note: Rate limiting would require Security package dependency, so we skip it here

        // Wrap with caching if provided
        if (_responseCache != null)
        {
            var cachedChain = chain;
            chain = new Runnable<TInput, TOutput>(async (input, ct) =>
            {
                // Try to get from cache first
                var cached = await _responseCache.GetCachedResponseAsync(input, ct).ConfigureAwait(false);
                if (cached != null)
                {
                    return cached;
                }

                // Execute and cache result
                var result = await cachedChain.InvokeAsync(input, null, ct).ConfigureAwait(false);
                await _responseCache.CacheResponseAsync(input, result, cancellationToken: ct).ConfigureAwait(false);
                return result;
            });
        }

        // Wrap with retry logic if configured
        if (_maxRetries > 0)
        {
            var retryChain = chain;
            chain = new Runnable<TInput, TOutput>(async (input, ct) =>
            {
                Exception? lastException = null;
                for (int attempt = 0; attempt <= _maxRetries; attempt++)
                {
                    try
                    {
                        return await retryChain.InvokeAsync(input, null, ct).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (attempt < _maxRetries)
                    {
                        lastException = ex;
                        if (_retryDelay.HasValue)
                        {
                            await Task.Delay(_retryDelay.Value, ct).ConfigureAwait(false);
                        }
                    }
                }

                throw new AgentException(
                    $"Chain execution failed after {_maxRetries + 1} attempts.",
                    ErrorCategory.LLMError,
                    lastException);
            });
        }

        return chain;
    }
}
