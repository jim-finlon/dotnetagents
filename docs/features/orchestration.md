# Feature: Orchestration

Use orchestration when one agent should not own every role in the process.
Separate roles make the handoffs, checks, and approvals visible.

## Good Fits

- writer, editor, and judge loops
- planner, executor, and verifier flows
- specialist delegation
- preview/confirm approval paths
- bounded human-in-the-loop checkpoints

## Basic Shape

```csharp
public sealed record Handoff<TPayload>(
    string FromRole,
    string ToRole,
    TPayload Payload,
    IReadOnlyList<string> EvidenceRefs);
```

Keep handoffs typed. The receiving role should know what it is checking and
what evidence it must return.

## Runnable Example

The public examples repository includes deterministic orchestration examples:

```bash
dotnet run --project examples/orchestration -- --smoke
```

The pack includes writer/editor/judge, planner/executor/verifier, and
preview/confirm approval examples.

## Implementation Checklist

- name each role explicitly
- keep role inputs and outputs typed
- separate execution from verification
- require preview/confirm before high-impact actions
- record evidence refs at role boundaries
- keep smoke mode deterministic

## Related Packages

- `DotNetAgents.Core`
- `DotNetAgents.Runtime`
- `DotNetAgents.PreviewConfirm`
- `DotNetAgents.Observability`
