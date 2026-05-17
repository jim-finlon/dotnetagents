using System.Text.Json;
using DotNetAgents.Abstractions.Models;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Voice.IntentClassification;

/// <summary>
/// LLM-based intent classifier that uses a language model to classify voice commands.
/// </summary>
public class LLMIntentClassifier : IIntentClassifier
{
    private readonly ILLMModel<ChatMessage[], ChatMessage> _llmModel;
    private readonly ILogger<LLMIntentClassifier> _logger;
    private readonly IIntentTaxonomyRegistry _taxonomyRegistry;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="LLMIntentClassifier"/> class.
    /// </summary>
    /// <param name="llmModel">The LLM model to use for classification.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="taxonomyRegistry">Optional taxonomy registry. If not provided, uses default static taxonomy.</param>
    public LLMIntentClassifier(
        ILLMModel<ChatMessage[], ChatMessage> llmModel,
        ILogger<LLMIntentClassifier> logger,
        IIntentTaxonomyRegistry? taxonomyRegistry = null)
    {
        _llmModel = llmModel ?? throw new ArgumentNullException(nameof(llmModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _taxonomyRegistry = taxonomyRegistry ?? new IntentTaxonomyRegistry();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc />
    public async Task<Intent> ClassifyAsync(
        string commandText,
        IntentContext? context = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(commandText))
        {
            throw new ArgumentException("Command text cannot be null or empty", nameof(commandText));
        }

        _logger.LogInformation("Classifying intent for command: {CommandText}", commandText);

        try
        {
            var taxonomy = _taxonomyRegistry.GetTaxonomy();
            var systemPrompt = GetSystemPrompt(taxonomy, context);
            var userPrompt = GetUserPrompt(commandText, context);

            var messages = new[]
            {
                ChatMessage.System(systemPrompt),
                ChatMessage.User(userPrompt)
            };

            var options = new LLMOptions
            {
                Temperature = 0.1, // Low temperature for consistent classification
                MaxTokens = 500
            };

            var response = await _llmModel.GenerateAsync(messages, options, cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(response.Content))
            {
                _logger.LogWarning("LLM returned empty response for command: {CommandText}", commandText);
                return CreateFallbackIntent(commandText);
            }

            var classification = JsonSerializer.Deserialize<IntentClassificationResponse>(
                response.Content,
                _jsonOptions);

            if (classification == null)
            {
                _logger.LogWarning("Failed to parse intent classification from response: {Response}", response.Content);
                return CreateFallbackIntent(commandText);
            }

            // Map intent name to domain/action/service
            var (domain, action, subType) = ParseIntentName(classification.Intent);
            var targetService = _taxonomyRegistry.GetTargetService(domain);

            _logger.LogInformation(
                "LLMIntentClassifier: raw_intent={IntentName} domain={Domain} action={Action} targetService={TargetService} tool={Tool} confidence={Confidence:F2}",
                classification.Intent,
                domain,
                action,
                targetService,
                classification.Tool,
                classification.Confidence);

            // Convert JsonElement parameters to proper types
            var parameters = ConvertParameters(classification.Parameters);

            // Apply context to parameters if available
            if (context != null && !context.IsEmpty)
            {
                ApplyContextToParameters(parameters, context);
            }

            // Determine missing required parameters using registry
            var requiredParams = _taxonomyRegistry.GetRequiredParameters(domain, action, subType);
            var missingRequired = requiredParams
                .Where(param => !parameters.ContainsKey(param))
                .ToList();

            return new Intent
            {
                Domain = domain,
                Action = action,
                SubType = subType,
                Parameters = parameters,
                MissingRequired = missingRequired,
                Confidence = classification.Confidence,
                TargetService = targetService,
                Tool = classification.Tool,
                RawText = commandText
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to classify intent for command: {CommandText}", commandText);
            return CreateFallbackIntent(commandText);
        }
    }

    private string GetSystemPrompt(IntentTaxonomy taxonomy, IntentContext? context)
    {
        var domains = taxonomy.Domains.Select(d => $"- {d.Key}: {d.Value.Description ?? "No description"}");
        var domainsText = string.Join("\n", domains);

        var contextInfo = context != null && !context.IsEmpty
            ? $"""

            Current Context:
            - Current Activity ID: {context.CurrentActivityId?.ToString() ?? "None"}
            - Current Goal ID: {context.CurrentGoalId?.ToString() ?? "None"}
            - Current Plan ID: {context.CurrentPlanId?.ToString() ?? "None"}
            - Current Time: {context.CurrentTime:yyyy-MM-dd HH:mm:ss}

            When classifying commands, use context to resolve ambiguous references:
            - "complete this" refers to current activity if available
            - "add to plan" refers to current plan if available
            - "for this goal" refers to current goal if available
            """
            : string.Empty;

        var memoryInfo = context is { LongTermUserMemory: { Length: > 0 } mem }
            ? $"""

            Long-term user memory (honor when interpreting commands; user may override in this utterance):
            {mem}
            """
            : string.Empty;

        return $$"""
            You are an intent classifier for a voice-controlled AI assistant called JARVIS.
            Your task is to classify user commands into structured intents.

            Supported domains:
            {{domainsText}}

            Supported actions:
            - create, list, update, delete, query, analyze, generate, schedule, reminder
            {{contextInfo}}
            {{memoryInfo}}

            Output format (JSON):
            {
                "intent": "domain.action" or "domain.action.subtype",
                "confidence": 0.0-1.0,
                "parameters": { "key": "value" },
                "tool": "optional_tool_name"
            }

            Extract all parameters from the command. If a required parameter is missing,
            do not include it in the parameters object (it will be tracked separately).

            Examples:
            - "create a note about the meeting" -> { "intent": "notes.create", "parameters": { "content": "meeting" } }
            - "schedule meeting with John tomorrow at 2pm" -> { "intent": "calendar.create_event", "parameters": { "title": "meeting", "attendee": "John", "date": "tomorrow", "time": "2pm" } }
            - "create invoice for Acme Corp for five thousand dollars" -> { "intent": "business.create_invoice", "parameters": { "client": "Acme Corp", "amount": 5000 } }
            """;
    }

    private static string GetUserPrompt(string commandText, IntentContext? context)
    {
        return $"Classify this command: \"{commandText}\"\n\nRespond with JSON only.";
    }

    private static void ApplyContextToParameters(Dictionary<string, object> parameters, IntentContext context)
    {
        // If command references "this" or "current", apply context IDs
        if (context.CurrentActivityId.HasValue && !parameters.ContainsKey("activity_id"))
        {
            // Check if command might refer to current activity
            // This is a simple heuristic - could be enhanced with LLM
            if (parameters.ContainsKey("this") || parameters.ContainsKey("current"))
            {
                parameters["activity_id"] = context.CurrentActivityId.Value;
            }
        }

        if (context.CurrentGoalId.HasValue && !parameters.ContainsKey("goal_id"))
        {
            if (parameters.ContainsKey("this_goal") || parameters.ContainsKey("current_goal"))
            {
                parameters["goal_id"] = context.CurrentGoalId.Value;
            }
        }

        if (context.CurrentPlanId.HasValue && !parameters.ContainsKey("plan_id"))
        {
            if (parameters.ContainsKey("this_plan") || parameters.ContainsKey("current_plan"))
            {
                parameters["plan_id"] = context.CurrentPlanId.Value;
            }
        }

        // Add current time if not specified
        if (!parameters.ContainsKey("time") && !parameters.ContainsKey("date"))
        {
            parameters["current_time"] = context.CurrentTime;
        }
    }

    private static (string Domain, string Action, string? SubType) ParseIntentName(string intentName)
    {
        if (string.IsNullOrWhiteSpace(intentName))
        {
            return ("unknown", "unknown", null);
        }

        var parts = intentName.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return ("unknown", "unknown", null);
        }

        if (parts.Length == 1)
        {
            return ("unknown", parts[0], null);
        }

        // Handle domain.action or domain.action_subtype format
        var domain = parts[0];
        var actionPart = parts[1];

        // Check if action contains underscore (e.g., "create_personal")
        var actionSubParts = actionPart.Split('_', 2, StringSplitOptions.RemoveEmptyEntries);
        if (actionSubParts.Length == 2)
        {
            // Format: domain.action_subtype
            return (domain, actionSubParts[0], actionSubParts[1]);
        }

        // Format: domain.action or domain.action.subtype
        if (parts.Length == 2)
        {
            return (domain, actionPart, null);
        }

        // Format: domain.action.subtype (3+ parts)
        return (domain, actionPart, parts[2]);
    }

    private static Dictionary<string, object> ConvertParameters(Dictionary<string, JsonElement>? jsonParameters)
    {
        if (jsonParameters == null)
        {
            return new Dictionary<string, object>();
        }

        var result = new Dictionary<string, object>();
        foreach (var (key, value) in jsonParameters)
        {
            // Convert JsonElement to appropriate type
            if (value is JsonElement jsonElement)
            {
                result[key] = jsonElement.ValueKind switch
                {
                    JsonValueKind.String => jsonElement.GetString() ?? string.Empty,
                    JsonValueKind.Number => jsonElement.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null!,
                    JsonValueKind.Array => jsonElement.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToArray(),
                    _ => jsonElement.GetRawText()
                };
            }
            else
            {
                result[key] = value;
            }
        }

        return result;
    }

    private static Intent CreateFallbackIntent(string commandText)
    {
        return new Intent
        {
            Domain = "unknown",
            Action = "unknown",
            Parameters = new Dictionary<string, object> { ["raw_text"] = commandText },
            MissingRequired = new List<string>(),
            Confidence = 0.0,
            RawText = commandText
        };
    }

    private sealed record IntentClassificationResponse
    {
        public string Intent { get; init; } = string.Empty;
        public double Confidence { get; init; }
        public Dictionary<string, JsonElement>? Parameters { get; init; }
        public string? Tool { get; init; }
    }
}
