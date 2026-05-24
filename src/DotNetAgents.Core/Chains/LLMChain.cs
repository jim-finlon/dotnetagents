// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.Chains;
using DotNetAgents.Abstractions.Models;
using DotNetAgents.Abstractions.OutputParsers;
using DotNetAgents.Abstractions.Prompts;

namespace DotNetAgents.Core.Chains;

/// <summary>
/// Chain that combines a prompt template with an LLM model and optional output parser.
/// </summary>
/// <typeparam name="TInput">The input type (typically IDictionary&lt;string, object&gt; for prompt variables).</typeparam>
/// <typeparam name="TOutput">The output type.</typeparam>
public class LLMChain<TInput, TOutput> : IRunnable<TInput, TOutput>
{
    private readonly IPromptTemplate _promptTemplate;
    private readonly ILLMModel<string, string> _llm;
    private readonly IOutputParser<TOutput>? _outputParser;

    /// <summary>
    /// Initializes a new instance of the <see cref="LLMChain{TInput, TOutput}"/> class.
    /// </summary>
    /// <param name="promptTemplate">The prompt template to use.</param>
    /// <param name="llm">The LLM model to use.</param>
    /// <param name="outputParser">Optional output parser. If null, raw string output is returned.</param>
    /// <exception cref="ArgumentNullException">Thrown when promptTemplate or llm is null.</exception>
    public LLMChain(
        IPromptTemplate promptTemplate,
        ILLMModel<string, string> llm,
        IOutputParser<TOutput>? outputParser = null)
    {
        _promptTemplate = promptTemplate ?? throw new ArgumentNullException(nameof(promptTemplate));
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _outputParser = outputParser;
    }

    /// <inheritdoc/>
    public async Task<TOutput> InvokeAsync(
        TInput input,
        DotNetAgents.Abstractions.Chains.RunnableOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = ChainTracing.StartActivity(
            "chain.llm.invoke",
            "llm",
            options,
            a =>
            {
                a.SetTag("model.id", _llm.ModelName);
                a.SetTag("prompt.template.length", _promptTemplate.Template.Length);
            });

        // Convert input to dictionary if needed
        var variables = ConvertToDictionary(input);

        // Format the prompt
        var formattedPrompt = await _promptTemplate.FormatAsync(variables, cancellationToken).ConfigureAwait(false);

        // Add format instructions if output parser is provided
        if (_outputParser != null)
        {
            var formatInstructions = _outputParser.GetFormatInstructions();
            if (!string.IsNullOrWhiteSpace(formatInstructions))
            {
                formattedPrompt = $"{formattedPrompt}\n\n{formatInstructions}";
            }
        }

        // Call LLM
        var rawOutput = await _llm.GenerateAsync(formattedPrompt, cancellationToken: cancellationToken).ConfigureAwait(false);

        // Parse output if parser is provided
        if (_outputParser != null)
        {
            return await _outputParser.ParseAsync(rawOutput, cancellationToken).ConfigureAwait(false);
        }

        // Return raw output (cast to TOutput - typically string)
        return (TOutput)(object)rawOutput;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<TOutput> StreamAsync(
        TInput input,
        DotNetAgents.Abstractions.Chains.RunnableOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var activity = ChainTracing.StartActivity(
            "chain.llm.stream",
            "llm",
            options,
            a => a.SetTag("model.id", _llm.ModelName));

        // Convert input to dictionary if needed
        var variables = ConvertToDictionary(input);

        // Format the prompt
        var formattedPrompt = await _promptTemplate.FormatAsync(variables, cancellationToken).ConfigureAwait(false);

        // Add format instructions if output parser is provided
        if (_outputParser != null)
        {
            var formatInstructions = _outputParser.GetFormatInstructions();
            if (!string.IsNullOrWhiteSpace(formatInstructions))
            {
                formattedPrompt = $"{formattedPrompt}\n\n{formatInstructions}";
            }
        }

        // Stream from LLM
        await foreach (var chunk in _llm.GenerateStreamAsync(formattedPrompt, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            // For streaming, we yield chunks as-is (parsing would require buffering)
            // In a real implementation, you might want to buffer and parse complete outputs
            yield return (TOutput)(object)chunk;
        }
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

    private static IDictionary<string, object> ConvertToDictionary(TInput input)
    {
        if (input is IDictionary<string, object> dict)
        {
            return dict;
        }

        // Try to convert using reflection or other means
        // For now, throw if not a dictionary
        throw new ArgumentException(
            $"Input must be of type IDictionary<string, object>, but got {typeof(TInput).Name}",
            nameof(input));
    }
}
