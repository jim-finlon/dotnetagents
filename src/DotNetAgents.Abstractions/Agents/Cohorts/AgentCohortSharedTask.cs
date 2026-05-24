// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Abstractions.Agents.Cohorts;

/// <summary>
/// Shared task context presented to every cohort member unless a member overrides the input.
/// </summary>
public sealed record AgentCohortSharedTask
{
    /// <summary>
    /// Gets the stable task id.
    /// </summary>
    public required string TaskId { get; init; }

    /// <summary>
    /// Gets the shared task input.
    /// </summary>
    public required string Input { get; init; }

    /// <summary>
    /// Gets optional shared context for the run.
    /// </summary>
    public string? Context { get; init; }

    /// <summary>
    /// Gets optional corpus reference used by Evaluation Sandbox or evaluation systems.
    /// </summary>
    public string? CorpusRef { get; init; }

    /// <summary>
    /// Gets non-secret task tags.
    /// </summary>
    public IReadOnlyDictionary<string, string> Tags { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Validates the task shape.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(TaskId))
        {
            throw new ArgumentException("Cohort task id is required.", nameof(TaskId));
        }

        if (string.IsNullOrWhiteSpace(Input))
        {
            throw new ArgumentException("Cohort task input is required.", nameof(Input));
        }
    }
}
