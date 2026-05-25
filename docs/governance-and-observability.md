# Governance And Observability

Production agents need more than capability. They need constraints and evidence.

## Start With Blast Radius

Classify actions before exposing them:

| Action Type | Example | Minimum Control |
| --- | --- | --- |
| Read-only | search, summarize, classify | argument validation and logging |
| Low-impact write | create a draft, save local note | validation and undo path |
| External write | send message, create ticket | preview/confirm and audit |
| Financial or destructive | delete, purchase, deploy | approval and rollback plan |
| Sensitive data | private docs, user data | data policy and redaction |

## Preview/Confirm

Agents should not jump directly from reasoning to high-impact mutation. A
preview/confirm flow lets the system show intended changes, collect approval,
and record what was approved.

Good preview output includes:

- target resource
- intended change
- expected side effects
- validation status
- rollback or undo note
- correlation id

## Observability

Use normal .NET observability practices:

- structured logs
- OpenTelemetry traces
- metrics for success, failure, retries, latency, and tool usage
- run ids that connect model calls, tool calls, and workflow steps
- redaction before logs leave the process

For GenAI telemetry, record enough to diagnose behavior without storing raw
secrets or unnecessary private data.

## Evidence For Improvement

An agent system improves when evidence changes the next version. Useful
evidence includes:

- failed tasks
- invalid tool arguments
- low-confidence outputs
- human corrections
- regression tests
- policy denials
- replayable trajectories

Keep the loop controlled: evidence should produce code, config, prompt,
workflow, or test changes that can be reviewed and released.

Premium packages may provide managed evidence receipts, certification, and
operations dashboards. The public core provides the framework-level building
blocks.
