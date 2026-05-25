# Feature: Behavior Trees

Use behavior trees when an agent needs tactical decision logic: try this, check
that, fall back, repeat, or stop.

## Good Fits

- support assistant chooses whether to search docs, ask a clarifying question,
  or escalate
- research assistant tries cached sources before live retrieval
- browser automation retries a safe read step before failing
- game or simulation agent chooses actions from conditions

## Why Use A Behavior Tree

Behavior trees make decision logic composable. They are easier to inspect than a
large prompt and easier to modify than a hard-coded chain of if/else blocks.

## Common Node Types

| Node | Purpose |
| --- | --- |
| Selector | try children until one succeeds |
| Sequence | run children in order until one fails |
| Condition | check state without side effects |
| Action | perform bounded work |
| Decorator | add retry, timeout, inversion, or policy |

## Basic Shape

```csharp
var tree = Selector(
    Sequence(HasKnownAnswer(), DraftAnswer()),
    Sequence(CanSearchDocs(), SearchDocs(), DraftAnswer()),
    EscalateToHuman());
```

The exact builder API may change during preview, but the pattern is stable:
conditions are cheap and side-effect-free; actions are explicit and bounded.

## Implementation Checklist

- keep conditions side-effect-free
- add timeout/retry decorators around unreliable actions
- log node outcomes
- test fallback behavior
- keep high-impact actions behind preview/confirm

## Related Packages

- `DotNetAgents.Agents.BehaviorTrees`
- `DotNetAgents.Agents.Supervisor.BehaviorTrees`
- `DotNetAgents.Runtime`
- `DotNetAgents.Observability`
