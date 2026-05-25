<!-- SPDX-License-Identifier: Apache-2.0 -->

# DotNetAgents.Memory.Advanced

`DotNetAgents.Memory.Advanced` owns local, assembly-time memory structures:
working memory, layered memory, and bounded prompt-memory snapshots.

## Bounded Prompt Memory

`BoundedMemorySnapshot` is a lightweight prompt-assembly input. It is not the
durable memory source of truth.

- A durable session store remains authoritative for resumable session context,
  tasks, and continuity state.
- A governed knowledge store remains authoritative for reusable cross-project
  lessons and patterns.
- **Bounded prompt memory** is a small, policy-checked snapshot assembled for a
  model turn and safe to inject into prompt context under a configured character
  or token budget.

Key contracts:

- `IMemorySnapshotProvider` builds `MemorySnapshot` values with scope, source,
  max budget, content hash, last-reviewed timestamp, policy id, redaction
  receipt, audit receipt, and source ids.
- `IMemorySearchProvider` exposes transcript search over runtime/session
  messages without deciding where durable transcripts live.
- `IMemoryWritePolicy` evaluates proposed memory writes for secrets,
  low-value chatter, unstable facts, and over-budget updates.
- `IMemoryLeakScrubber` removes provider-private tags and blocks before text is
  streamed back to users.

Runtime sessions should bind transcript documents to the originating
`AgentSession.Id` and, when coming from a gateway channel, the
`GatewaySessionKey.StableKey` metadata. Durable synchronization should happen
outside this package through your application's session and knowledge-store
clients.
