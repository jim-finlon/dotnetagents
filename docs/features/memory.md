# Feature: Memory

Memory gives an agent context beyond the current prompt. Treat memory as
multiple stores with different jobs.

## Memory Types

| Type | Purpose |
| --- | --- |
| prompt memory | bounded context for one model turn |
| session memory | resumable state for one user or run |
| knowledge memory | reusable lessons, facts, or patterns |
| profile memory | preferences and stable user context |
| audit memory | evidence of what happened |

## Why Separate Them

Mixing every memory type into one store creates privacy, quality, and debugging
problems. A prompt needs a small relevant snapshot. An audit trail needs durable
evidence. A knowledge store needs curation.

## Basic Shape

```csharp
public sealed record MemorySnippet(
    string Source,
    string Text,
    int Priority,
    DateTimeOffset LastReviewed);
```

Build a bounded snapshot before each model turn and keep durable writes behind a
policy.

## Implementation Checklist

- cap prompt memory size
- record source ids
- redact before prompt injection
- reject low-value memory writes
- keep private data out of shared knowledge stores
- test memory selection deterministically

## Related Packages

- `DotNetAgents.Memory.Advanced`
- `DotNetAgents.Memory.Profile`
- `DotNetAgents.SessionPersistence`
- `DotNetAgents.Knowledge`
- `DotNetAgents.Security`
