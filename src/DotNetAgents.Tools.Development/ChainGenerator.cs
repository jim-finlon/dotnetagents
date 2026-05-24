// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DotNetAgents.Tools.Development;

/// <summary>
/// Generates chain code from natural language descriptions using AI.
/// </summary>
public class ChainGenerator
{
    private readonly ILLMModel<string, string> _llmModel;
    private readonly ILogger<ChainGenerator>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainGenerator"/> class.
    /// </summary>
    /// <param name="llmModel">The LLM model to use for code generation.</param>
    /// <param name="logger">Optional logger instance.</param>
    public ChainGenerator(
        ILLMModel<string, string> llmModel,
        ILogger<ChainGenerator>? logger = null)
    {
        _llmModel = llmModel ?? throw new ArgumentNullException(nameof(llmModel));
        _logger = logger;
    }

    /// <summary>
    /// Generates chain code from a natural language description.
    /// </summary>
    /// <param name="description">Natural language description of the desired chain.</param>
    /// <param name="options">Optional generation options.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Generated chain code and metadata.</returns>
    public async Task<ChainGenerationResult> GenerateAsync(
        string description,
        ChainGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        options ??= new ChainGenerationOptions();

        var prompt = BuildPrompt(description, options);

        _logger?.LogInformation("Generating chain from description: {Description}", description);

        try
        {
            var response = await _llmModel.GenerateAsync(prompt, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var result = ParseResponse(response);
            result.Description = description;

            _logger?.LogInformation("Chain generation completed. Generated {NodeCount} nodes", result.Nodes.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to generate chain from description");
            throw new ChainGenerationException("Failed to generate chain from description", ex);
        }
    }

    private string BuildPrompt(string description, ChainGenerationOptions options)
    {
        return $@"You are an expert C# developer specializing in DotNetAgents chains. Generate a chain definition based on the following description.

Description: {description}

Requirements:
- Use DotNetAgents chain patterns
- Include proper error handling
- Add appropriate logging
- Use async/await patterns
- Follow C# best practices

Additional context:
- Target framework: .NET 10
- Use nullable reference types
- Prefer records for DTOs

Generate a JSON response with the following structure:
{{
  ""chainType"": ""llm|retrieval|sequential"",
  ""nodes"": [
    {{
      ""name"": ""node-name"",
      ""type"": ""llm|prompt|retrieval|transform"",
      ""config"": {{}}
    }}
  ],
  ""code"": ""// Generated C# code here"",
  ""explanation"": ""Brief explanation of the generated chain""
}}

Generate the chain definition now:";
    }

    private ChainGenerationResult ParseResponse(string response)
    {
        try
        {
            // Try to extract JSON from markdown code blocks if present
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}') + 1;

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.Substring(jsonStart, jsonEnd - jsonStart);
                var parsed = JsonSerializer.Deserialize<ChainGenerationResponse>(json);

                if (parsed != null)
                {
                    return new ChainGenerationResult
                    {
                        ChainType = parsed.ChainType ?? "llm",
                        Nodes = parsed.Nodes ?? new List<ChainNodeDefinition>(),
                        GeneratedCode = parsed.Code ?? string.Empty,
                        Explanation = parsed.Explanation ?? string.Empty
                    };
                }
            }

            // Fallback: return raw response as code
            return new ChainGenerationResult
            {
                ChainType = "llm",
                Nodes = new List<ChainNodeDefinition>(),
                GeneratedCode = response,
                Explanation = "Generated from LLM response"
            };
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to parse LLM response as JSON, using raw response");
            return new ChainGenerationResult
            {
                ChainType = "llm",
                Nodes = new List<ChainNodeDefinition>(),
                GeneratedCode = response,
                Explanation = "Raw LLM response (parsing failed)"
            };
        }
    }
}

/// <summary>
/// Options for chain generation.
/// </summary>
public class ChainGenerationOptions
{
    /// <summary>
    /// Gets or sets whether to include error handling.
    /// </summary>
    public bool IncludeErrorHandling { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include logging.
    /// </summary>
    public bool IncludeLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets additional context for generation.
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// Result of chain generation.
/// </summary>
public class ChainGenerationResult
{
    /// <summary>
    /// Gets or sets the chain type.
    /// </summary>
    public string ChainType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of nodes in the chain.
    /// </summary>
    public List<ChainNodeDefinition> Nodes { get; set; } = new();

    /// <summary>
    /// Gets or sets the generated C# code.
    /// </summary>
    public string GeneratedCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the explanation of the generated chain.
    /// </summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Definition of a chain node.
/// </summary>
public class ChainNodeDefinition
{
    /// <summary>
    /// Gets or sets the node name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the node type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the node configuration.
    /// </summary>
    public Dictionary<string, object> Config { get; set; } = new();
}

/// <summary>
/// Internal response structure from LLM.
/// </summary>
internal class ChainGenerationResponse
{
    public string? ChainType { get; set; }
    public List<ChainNodeDefinition>? Nodes { get; set; }
    public string? Code { get; set; }
    public string? Explanation { get; set; }
}

/// <summary>
/// Exception thrown when chain generation fails.
/// </summary>
public class ChainGenerationException : Exception
{
    public ChainGenerationException(string message) : base(message) { }
    public ChainGenerationException(string message, Exception innerException) : base(message, innerException) { }
}
