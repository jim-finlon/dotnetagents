// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Governance.Workflows;

/// <summary>
/// Contract for a single step in an agent workflow. Runners dispatch steps by
/// <see cref="Kind"/> and enforce <see cref="MemoryScopes"/> before invoking <see cref="ExecuteAsync"/>.
/// Implementations live in the service that owns the step (e.g. a Retrieve-step for
/// knowledge-memory lessons lives alongside the knowledge-memory client).
/// </summary>
public interface IWorkflowStep
{
    /// <summary>Stable id within a workflow definition. Human-readable — used in logs and UI.</summary>
    string Id { get; }

    /// <summary>Primitive shape of this step. Runners use this to decide dispatch semantics.</summary>
    WorkflowStepKind Kind { get; }

    /// <summary>Memory surfaces this step is allowed to touch (may be empty for pure compute).</summary>
    IReadOnlyList<MemoryScope> MemoryScopes { get; }

    /// <summary>
    /// Execute the step. The runner sets <see cref="Identity.InvokerContextAccessor"/> before
    /// calling so implementations can access ambient <see cref="Identity.InvokerContext"/>.
    /// Input/output are untyped so workflows can chain arbitrary shapes; individual step
    /// implementations are responsible for type-checking their payloads.
    /// </summary>
    Task<WorkflowStepResult> ExecuteAsync(WorkflowStepInput input, CancellationToken ct);
}

/// <summary>Input envelope handed to <see cref="IWorkflowStep.ExecuteAsync"/>.</summary>
/// <param name="Payload">Upstream step output (or the workflow trigger payload for the first step).</param>
/// <param name="Metadata">Workflow-run-scoped metadata (correlation id, retry count, etc.).</param>
public sealed record WorkflowStepInput(object? Payload, IReadOnlyDictionary<string, string> Metadata);

/// <summary>Output envelope produced by <see cref="IWorkflowStep.ExecuteAsync"/>.</summary>
/// <param name="Payload">Data for the next step or for the Respond step to surface.</param>
/// <param name="Succeeded">False when the step failed in a way the runner should treat as terminal.</param>
/// <param name="ErrorMessage">Populated when <paramref name="Succeeded"/> is false.</param>
public sealed record WorkflowStepResult(object? Payload, bool Succeeded, string? ErrorMessage = null);
