# Feature: Runtime Trajectories

Runtime trajectories capture what happened during an agent run.

## What To Capture

- session id
- task or goal
- model calls
- tool calls
- selected context
- intermediate messages
- output artifacts
- validation results
- errors and retries

## Why It Matters

Without trajectory evidence, failures become anecdotes. With trajectories, you
can replay, debug, compare, and improve agent behavior.

## Basic Shape

```csharp
public sealed record AgentRunEvent(
    string RunId,
    string Kind,
    DateTimeOffset Timestamp,
    string Summary,
    IReadOnlyDictionary<string, string> Metadata);
```

Keep metadata useful but safe. Do not store raw secrets or unnecessary private
payloads.

## Implementation Checklist

- assign a run id before work starts
- correlate model calls and tool calls
- redact sensitive values
- record validation outcomes
- store artifacts by reference when payloads are large
- make tests assert that important events are emitted

## Related Packages

- `DotNetAgents.Runtime`
- `DotNetAgents.Observability`
- `DotNetAgents.Observability.GenAi`
- `DotNetAgents.Memory.Advanced`
