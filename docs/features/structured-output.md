# Feature: Structured Output

Structured output turns model responses into typed data that application code
can validate.

## Good Fits

- classification
- extraction
- routing decisions
- tool arguments
- document summaries
- workflow state updates

## Why Use It

Free-form text is useful for humans but hard for software to trust. Structured
output lets you validate required fields, reject malformed responses, and write
tests.

## Basic Shape

```csharp
public sealed record TriageDecision(
    string Severity,
    string Category,
    string SuggestedNextAction,
    double Confidence);
```

Validate the result before acting:

```csharp
if (decision.Confidence < 0.7)
{
    return TriageOutcome.NeedsReview(decision);
}
```

## Implementation Checklist

- define a typed result model
- reject missing required fields
- bound numeric values
- treat low confidence as a workflow state
- keep raw model text out of logs unless it is safe
- test malformed JSON and missing fields

## Related Packages

- `DotNetAgents.StructuredOutput`
- `DotNetAgents.PromptRuntime`
- `DotNetAgents.Runtime`
- `DotNetAgents.Security`
