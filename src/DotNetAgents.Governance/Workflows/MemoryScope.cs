// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Governance.Workflows;

/// <summary>
/// Well-known memory surfaces a workflow step may touch. Each step declares which
/// surfaces it can read and/or write so the runner enforces least-privilege access.
/// </summary>
public enum MemorySurface
{
    /// <summary>Transient per-step scratch; never persisted.</summary>
    StepLocal = 0,
    /// <summary>The current agent session (conversation turn buffer, tool results).</summary>
    Session = 1,
    /// <summary>Agent-scoped long-term state (user preferences, running totals).</summary>
    AgentState = 2,
    /// <summary>Project/workspace knowledge (knowledge-memory lessons, retrieval corpus).</summary>
    Workspace = 3,
    /// <summary>Global durable memory (shared across agents within a tenant).</summary>
    Global = 4
}

/// <summary>
/// Declaration of which memory surfaces a step may read from or write to. Constructed
/// by the workflow author; enforced by the runner at step dispatch.
/// </summary>
/// <param name="Surface">Which memory plane this scope refers to.</param>
/// <param name="CanRead">True if the step is allowed to read from this surface.</param>
/// <param name="CanWrite">True if the step is allowed to write to this surface.</param>
public sealed record MemoryScope(MemorySurface Surface, bool CanRead, bool CanWrite)
{
    public static MemoryScope ReadOnly(MemorySurface s) => new(s, CanRead: true, CanWrite: false);
    public static MemoryScope WriteOnly(MemorySurface s) => new(s, CanRead: false, CanWrite: true);
    public static MemoryScope ReadWrite(MemorySurface s) => new(s, CanRead: true, CanWrite: true);
}
