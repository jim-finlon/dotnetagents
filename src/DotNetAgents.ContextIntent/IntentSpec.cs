using System.Text.Json.Serialization;

namespace DotNetAgents.ContextIntent;

/// <summary>
/// What the task is trying to accomplish. Verb + one-sentence goal + optional success criteria.
/// </summary>
/// <param name="Verb">Canonical action verb in imperative form (e.g. "ship", "diagnose", "refactor", "document", "decide").</param>
/// <param name="Goal">One-sentence goal statement in natural language.</param>
/// <param name="SuccessCriteria">Optional list of what 'done' looks like, authored by the intent originator. Distinct from <see cref="ContextIntentEnvelope.Acceptance"/> which carries agent-authored acceptance tests.</param>
public sealed record IntentSpec(
    [property: JsonPropertyName("verb")] string Verb,
    [property: JsonPropertyName("goal")] string Goal,
    [property: JsonPropertyName("success_criteria")] IReadOnlyList<string>? SuccessCriteria = null);

/// <summary>
/// Canonical verbs the intent originator can choose from. Free-form strings are also allowed
/// in <see cref="IntentSpec.Verb"/> — these are conventional aliases for retrieval and routing.
/// </summary>
public static class IntentVerbs
{
    public const string Ship = "ship";
    public const string Diagnose = "diagnose";
    public const string Refactor = "refactor";
    public const string Document = "document";
    public const string Decide = "decide";
    public const string Investigate = "investigate";
    public const string Review = "review";
    public const string Test = "test";
    public const string Deploy = "deploy";
    public const string Onboard = "onboard";
}
