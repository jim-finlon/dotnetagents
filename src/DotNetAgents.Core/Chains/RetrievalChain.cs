using DotNetAgents.Abstractions.Chains;
using DotNetAgents.Abstractions.Documents;
using DotNetAgents.Abstractions.Models;
using DotNetAgents.Abstractions.Prompts;
using DotNetAgents.Abstractions.Retrieval;

namespace DotNetAgents.Core.Chains;

/// <summary>
/// Chain that implements Retrieval-Augmented Generation (RAG) pattern.
/// Retrieves relevant documents and injects them into a prompt template.
/// </summary>
/// <typeparam name="TInput">The input type (typically IDictionary&lt;string, object&gt; for prompt variables).</typeparam>
/// <typeparam name="TOutput">The output type.</typeparam>
public class RetrievalChain<TInput, TOutput> : IRunnable<TInput, TOutput>
{
    private readonly IPromptTemplate _promptTemplate;
    private readonly ILLMModel<string, string> _llm;
    private readonly IVectorStore _vectorStore;
    private readonly IEmbeddingModel _embeddingModel;
    private readonly int _topK;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetrievalChain{TInput, TOutput}"/> class.
    /// </summary>
    /// <param name="promptTemplate">The prompt template that includes a {context} variable for retrieved documents.</param>
    /// <param name="llm">The LLM model to use for generation.</param>
    /// <param name="vectorStore">The vector store to search.</param>
    /// <param name="embeddingModel">The embedding model to use for query embedding.</param>
    /// <param name="topK">The number of documents to retrieve (default: 5).</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    public RetrievalChain(
        IPromptTemplate promptTemplate,
        ILLMModel<string, string> llm,
        IVectorStore vectorStore,
        IEmbeddingModel embeddingModel,
        int topK = 5)
    {
        _promptTemplate = promptTemplate ?? throw new ArgumentNullException(nameof(promptTemplate));
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _vectorStore = vectorStore ?? throw new ArgumentNullException(nameof(vectorStore));
        _embeddingModel = embeddingModel ?? throw new ArgumentNullException(nameof(embeddingModel));

        if (topK <= 0)
            throw new ArgumentException("TopK must be positive.", nameof(topK));

        _topK = topK;
    }

    /// <inheritdoc/>
    public async Task<TOutput> InvokeAsync(
        TInput input,
        DotNetAgents.Abstractions.Chains.RunnableOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = ChainTracing.StartActivity(
            "chain.retrieval.invoke",
            "retrieval",
            options,
            a =>
            {
                a.SetTag("model.id", _llm.ModelName);
                a.SetTag("retrieval.top_k", _topK);
            });

        // Convert input to dictionary
        var variables = ConvertToDictionary(input);

        // Extract query from input (expect "query" key)
        if (!variables.TryGetValue("query", out var queryObj) || queryObj is not string query)
        {
            throw new ArgumentException("Input must contain a 'query' key with a string value.", nameof(input));
        }

        // Generate embedding for the query
        var queryEmbedding = await _embeddingModel.EmbedAsync(query, cancellationToken).ConfigureAwait(false);

        // Search vector store
        var searchResults = await _vectorStore.SearchAsync(
            queryEmbedding,
            topK: _topK,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        // Build context from retrieved documents
        var context = BuildContext(searchResults);

        // Add context to variables
        variables["context"] = context;

        // Format prompt with context
        var formattedPrompt = await _promptTemplate.FormatAsync(variables, cancellationToken).ConfigureAwait(false);

        // Call LLM
        var rawOutput = await _llm.GenerateAsync(formattedPrompt, cancellationToken: cancellationToken).ConfigureAwait(false);

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
            "chain.retrieval.stream",
            "retrieval",
            options,
            a => a.SetTag("model.id", _llm.ModelName));

        // For streaming, we retrieve once and stream the LLM response
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

    private static string BuildContext(IReadOnlyList<DotNetAgents.Abstractions.Retrieval.VectorSearchResult> searchResults)
    {
        if (searchResults.Count == 0)
        {
            return "No relevant documents found.";
        }

        var contextParts = new List<string>();
        for (int i = 0; i < searchResults.Count; i++)
        {
            var result = searchResults[i];
            var content = result.Metadata?.TryGetValue("content", out var contentObj) == true
                ? contentObj?.ToString() ?? string.Empty
                : $"Document {result.Id}";

            contextParts.Add($"[{i + 1}] {content}");
        }

        return string.Join("\n\n", contextParts);
    }

    private static IDictionary<string, object> ConvertToDictionary(TInput input)
    {
        if (input is IDictionary<string, object> dict)
        {
            return dict;
        }

        throw new ArgumentException(
            $"Input must be of type IDictionary<string, object>, but got {typeof(TInput).Name}",
            nameof(input));
    }
}
