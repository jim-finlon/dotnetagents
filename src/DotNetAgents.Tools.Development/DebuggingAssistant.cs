// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DotNetAgents.Tools.Development;

/// <summary>
/// AI-powered assistant for debugging workflow and chain execution issues.
/// </summary>
public class DebuggingAssistant
{
    private readonly ILLMModel<string, string> _llmModel;
    private readonly ILogger<DebuggingAssistant>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebuggingAssistant"/> class.
    /// </summary>
    /// <param name="llmModel">The LLM model to use for analysis.</param>
    /// <param name="logger">Optional logger instance.</param>
    public DebuggingAssistant(
        ILLMModel<string, string> llmModel,
        ILogger<DebuggingAssistant>? logger = null)
    {
        _llmModel = llmModel ?? throw new ArgumentNullException(nameof(llmModel));
        _logger = logger;
    }

    /// <summary>
    /// Analyzes workflow execution and suggests fixes.
    /// </summary>
    /// <param name="executionLog">The execution log or error information.</param>
    /// <param name="workflowDefinition">Optional workflow definition for context.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Debugging analysis with suggestions.</returns>
    public async Task<DebuggingAnalysis> AnalyzeAsync(
        string executionLog,
        object? workflowDefinition = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionLog);

        var prompt = BuildAnalysisPrompt(executionLog, workflowDefinition);

        _logger?.LogInformation("Analyzing execution log for debugging");

        try
        {
            var response = await _llmModel.GenerateAsync(prompt, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var analysis = ParseAnalysis(response);

            _logger?.LogInformation("Debugging analysis completed. Found {IssueCount} issues, {SuggestionCount} suggestions",
                analysis.Issues.Count, analysis.Suggestions.Count);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to analyze execution log");
            throw new DebuggingAnalysisException("Failed to analyze execution log", ex);
        }
    }

    /// <summary>
    /// Suggests optimizations for a workflow or chain.
    /// </summary>
    /// <param name="definition">The workflow or chain definition.</param>
    /// <param name="performanceMetrics">Optional performance metrics.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Optimization suggestions.</returns>
    public async Task<OptimizationSuggestions> SuggestOptimizationsAsync(
        string definition,
        Dictionary<string, object>? performanceMetrics = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(definition);

        var prompt = BuildOptimizationPrompt(definition, performanceMetrics);

        _logger?.LogInformation("Generating optimization suggestions");

        try
        {
            var response = await _llmModel.GenerateAsync(prompt, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var suggestions = ParseOptimizations(response);

            _logger?.LogInformation("Generated {SuggestionCount} optimization suggestions",
                suggestions.Suggestions.Count);

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to generate optimization suggestions");
            throw new DebuggingAnalysisException("Failed to generate optimization suggestions", ex);
        }
    }

    private string BuildAnalysisPrompt(string executionLog, object? workflowDefinition)
    {
        var workflowJson = workflowDefinition != null
            ? JsonSerializer.Serialize(workflowDefinition)
            : "Not provided";

        return $@"You are an expert debugging assistant for DotNetAgents workflows and chains. Analyze the following execution log and provide debugging insights.

Execution Log:
{executionLog}

Workflow Definition (if available):
{workflowJson}

Analyze the execution and provide:
1. Identified issues
2. Root cause analysis
3. Suggested fixes
4. Prevention strategies

Format your response as JSON:
{{
  ""issues"": [
    {{
      ""severity"": ""error|warning|info"",
      ""message"": ""Issue description"",
      ""location"": ""Where the issue occurs"",
      ""rootCause"": ""Root cause analysis""
    }}
  ],
  ""suggestions"": [
    {{
      ""type"": ""fix|optimization|prevention"",
      ""description"": ""Suggestion description"",
      ""code"": ""Optional code example"",
      ""priority"": ""high|medium|low""
    }}
  ],
  ""summary"": ""Overall analysis summary""
}}

Analyze now:";
    }

    private string BuildOptimizationPrompt(string definition, Dictionary<string, object>? metrics)
    {
        var metricsJson = metrics != null
            ? JsonSerializer.Serialize(metrics)
            : "Not provided";

        return $@"You are an expert performance optimization specialist for DotNetAgents. Analyze the following workflow/chain definition and suggest optimizations.

Definition:
{definition}

Performance Metrics:
{metricsJson}

Provide optimization suggestions focusing on:
1. Performance improvements
2. Resource usage reduction
3. Cost optimization
4. Scalability improvements

Format your response as JSON:
{{
  ""suggestions"": [
    {{
      ""type"": ""performance|cost|scalability|maintainability"",
      ""description"": ""Optimization description"",
      ""impact"": ""Expected impact"",
      ""effort"": ""low|medium|high"",
      ""code"": ""Optional code example""
    }}
  ],
  ""summary"": ""Overall optimization summary""
}}

Analyze now:";
    }

    private DebuggingAnalysis ParseAnalysis(string response)
    {
        try
        {
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}') + 1;

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.Substring(jsonStart, jsonEnd - jsonStart);
                var parsed = JsonSerializer.Deserialize<DebuggingAnalysisResponse>(json);

                if (parsed != null)
                {
                    return new DebuggingAnalysis
                    {
                        Issues = parsed.Issues ?? new List<DebuggingIssue>(),
                        Suggestions = parsed.Suggestions ?? new List<DebuggingSuggestion>(),
                        Summary = parsed.Summary ?? string.Empty
                    };
                }
            }

            // Fallback
            return new DebuggingAnalysis
            {
                Issues = new List<DebuggingIssue>(),
                Suggestions = new List<DebuggingSuggestion>(),
                Summary = response
            };
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to parse analysis response");
            return new DebuggingAnalysis
            {
                Issues = new List<DebuggingIssue>(),
                Suggestions = new List<DebuggingSuggestion>(),
                Summary = response
            };
        }
    }

    private OptimizationSuggestions ParseOptimizations(string response)
    {
        try
        {
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}') + 1;

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.Substring(jsonStart, jsonEnd - jsonStart);
                var parsed = JsonSerializer.Deserialize<OptimizationSuggestionsResponse>(json);

                if (parsed != null)
                {
                    return new OptimizationSuggestions
                    {
                        Suggestions = parsed.Suggestions ?? new List<OptimizationSuggestion>(),
                        Summary = parsed.Summary ?? string.Empty
                    };
                }
            }

            return new OptimizationSuggestions
            {
                Suggestions = new List<OptimizationSuggestion>(),
                Summary = response
            };
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to parse optimization response");
            return new OptimizationSuggestions
            {
                Suggestions = new List<OptimizationSuggestion>(),
                Summary = response
            };
        }
    }
}

/// <summary>
/// Result of debugging analysis.
/// </summary>
public class DebuggingAnalysis
{
    /// <summary>
    /// Gets or sets the list of identified issues.
    /// </summary>
    public List<DebuggingIssue> Issues { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of suggestions.
    /// </summary>
    public List<DebuggingSuggestion> Suggestions { get; set; } = new();

    /// <summary>
    /// Gets or sets the analysis summary.
    /// </summary>
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Represents a debugging issue.
/// </summary>
public class DebuggingIssue
{
    /// <summary>
    /// Gets or sets the issue severity.
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the issue message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the issue location.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the root cause analysis.
    /// </summary>
    public string RootCause { get; set; } = string.Empty;
}

/// <summary>
/// Represents a debugging suggestion.
/// </summary>
public class DebuggingSuggestion
{
    /// <summary>
    /// Gets or sets the suggestion type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the suggestion description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional code example.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Gets or sets the suggestion priority.
    /// </summary>
    public string Priority { get; set; } = "medium";
}

/// <summary>
/// Result of optimization analysis.
/// </summary>
public class OptimizationSuggestions
{
    /// <summary>
    /// Gets or sets the list of optimization suggestions.
    /// </summary>
    public List<OptimizationSuggestion> Suggestions { get; set; } = new();

    /// <summary>
    /// Gets or sets the summary.
    /// </summary>
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Represents an optimization suggestion.
/// </summary>
public class OptimizationSuggestion
{
    /// <summary>
    /// Gets or sets the optimization type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expected impact.
    /// </summary>
    public string Impact { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the implementation effort.
    /// </summary>
    public string Effort { get; set; } = "medium";

    /// <summary>
    /// Gets or sets optional code example.
    /// </summary>
    public string? Code { get; set; }
}

/// <summary>
/// Internal response structures.
/// </summary>
internal class DebuggingAnalysisResponse
{
    public List<DebuggingIssue>? Issues { get; set; }
    public List<DebuggingSuggestion>? Suggestions { get; set; }
    public string? Summary { get; set; }
}

internal class OptimizationSuggestionsResponse
{
    public List<OptimizationSuggestion>? Suggestions { get; set; }
    public string? Summary { get; set; }
}

/// <summary>
/// Exception thrown when debugging analysis fails.
/// </summary>
public class DebuggingAnalysisException : Exception
{
    public DebuggingAnalysisException(string message) : base(message) { }
    public DebuggingAnalysisException(string message, Exception innerException) : base(message, innerException) { }
}
