// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Workflow.Designer;
using DotNetAgents.Abstractions.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DotNetAgents.Tools.Development;

/// <summary>
/// Converts natural language descriptions to workflow definitions using AI.
/// </summary>
public class WorkflowBuilder
{
    private readonly ILLMModel<string, string> _llmModel;
    private readonly ILogger<WorkflowBuilder>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowBuilder"/> class.
    /// </summary>
    /// <param name="llmModel">The LLM model to use for workflow generation.</param>
    /// <param name="logger">Optional logger instance.</param>
    public WorkflowBuilder(
        ILLMModel<string, string> llmModel,
        ILogger<WorkflowBuilder>? logger = null)
    {
        _llmModel = llmModel ?? throw new ArgumentNullException(nameof(llmModel));
        _logger = logger;
    }

    /// <summary>
    /// Generates a workflow definition from a natural language description.
    /// </summary>
    /// <param name="description">Natural language description of the desired workflow.</param>
    /// <param name="options">Optional generation options.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Generated workflow definition.</returns>
    public async Task<WorkflowDefinitionDto> GenerateAsync(
        string description,
        WorkflowGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        options ??= new WorkflowGenerationOptions();

        var prompt = BuildPrompt(description, options);

        _logger?.LogInformation("Generating workflow from description: {Description}", description);

        try
        {
            var response = await _llmModel.GenerateAsync(prompt, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var workflow = ParseResponse(response, description);

            _logger?.LogInformation("Workflow generation completed. Generated {NodeCount} nodes, {EdgeCount} edges",
                workflow.Nodes.Count, workflow.Edges.Count);

            return workflow;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to generate workflow from description");
            throw new WorkflowGenerationException("Failed to generate workflow from description", ex);
        }
    }

    private string BuildPrompt(string description, WorkflowGenerationOptions options)
    {
        return $@"You are an expert workflow designer. Generate a DotNetAgents workflow definition from the following description.

Description: {description}

Generate a JSON workflow definition with the following structure:
{{
  ""name"": ""workflow-name"",
  ""description"": ""Workflow description"",
  ""version"": ""1.0.0"",
  ""nodes"": [
    {{
      ""id"": ""node-id"",
      ""name"": ""node-name"",
      ""type"": ""function|condition|parallel|human-in-loop"",
      ""x"": 100,
      ""y"": 100,
      ""config"": {{}},
      ""label"": ""Display Name"",
      ""description"": ""Node description""
    }}
  ],
  ""edges"": [
    {{
      ""id"": ""edge-id"",
      ""from"": ""source-node-id"",
      ""to"": ""target-node-id"",
      ""label"": ""Edge label"",
      ""conditional"": false,
      ""condition"": null
    }}
  ],
  ""entryPoint"": ""entry-node-id"",
  ""exitPoints"": [""exit-node-id""]
}}

Requirements:
- Include an entry point
- Include at least one exit point
- Connect all nodes with edges
- Use appropriate node types
- Position nodes logically (x, y coordinates)

Generate the workflow definition now:";
    }

    private WorkflowDefinitionDto ParseResponse(string response, string originalDescription)
    {
        try
        {
            // Try to extract JSON from markdown code blocks if present
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}') + 1;

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.Substring(jsonStart, jsonEnd - jsonStart);
                var parsed = JsonSerializer.Deserialize<WorkflowDefinitionDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (parsed != null)
                {
                    parsed.Description ??= originalDescription;
                    return parsed;
                }
            }

            // Fallback: create a simple workflow
            _logger?.LogWarning("Failed to parse LLM response, creating fallback workflow");
            return CreateFallbackWorkflow(originalDescription);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to parse workflow response, creating fallback");
            return CreateFallbackWorkflow(originalDescription);
        }
    }

    private WorkflowDefinitionDto CreateFallbackWorkflow(string description)
    {
        return new WorkflowDefinitionDto
        {
            Name = "generated-workflow",
            Description = description,
            Version = "1.0.0",
            Nodes = new List<WorkflowNodeDto>
            {
                new WorkflowNodeDto
                {
                    Id = "start",
                    Name = "start",
                    Type = "function",
                    X = 100,
                    Y = 100,
                    Label = "Start"
                },
                new WorkflowNodeDto
                {
                    Id = "end",
                    Name = "end",
                    Type = "function",
                    X = 300,
                    Y = 100,
                    Label = "End"
                }
            },
            Edges = new List<WorkflowEdgeDto>
            {
                new WorkflowEdgeDto
                {
                    Id = "edge-1",
                    From = "start",
                    To = "end"
                }
            },
            EntryPoint = "start",
            ExitPoints = new List<string> { "end" }
        };
    }
}

/// <summary>
/// Options for workflow generation.
/// </summary>
public class WorkflowGenerationOptions
{
    /// <summary>
    /// Gets or sets whether to include error handling nodes.
    /// </summary>
    public bool IncludeErrorHandling { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include human-in-the-loop nodes.
    /// </summary>
    public bool IncludeHumanInLoop { get; set; } = false;

    /// <summary>
    /// Gets or sets additional context for generation.
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// Exception thrown when workflow generation fails.
/// </summary>
public class WorkflowGenerationException : Exception
{
    public WorkflowGenerationException(string message) : base(message) { }
    public WorkflowGenerationException(string message, Exception innerException) : base(message, innerException) { }
}
