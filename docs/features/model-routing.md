# Feature: Model Routing

Model routing chooses which model or provider should handle a task.

## Good Fits

- cheap model for classification
- stronger model for planning or synthesis
- local model for private drafts
- provider fallback after timeout
- task-specific model selection

## Why Route

One model is rarely ideal for every job. Routing lets a system balance cost,
latency, privacy, reliability, and quality.

## Basic Shape

```csharp
public sealed record ModelRouteRequest(
    string TaskKind,
    int MaxLatencyMs,
    bool AllowsExternalProvider,
    string RequiredQuality);

public sealed record ModelRouteDecision(
    string Provider,
    string Model,
    string Reason);
```

The route decision should be observable. Record why a provider was selected.

## Implementation Checklist

- classify task kind before routing
- keep private-data policy separate from model preference
- define fallback behavior
- record cost and latency where available
- test routing rules without live providers
- avoid logging prompt payloads unnecessarily

## Related Packages

- `DotNetAgents.ModelRouting`
- `DotNetAgents.PromptRuntime`
- `DotNetAgents.Observability.GenAi`
- `DotNetAgents.Runtime`
