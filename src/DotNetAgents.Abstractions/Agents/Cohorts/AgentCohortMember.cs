// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Abstractions.Agents.Cohorts;

/// <summary>
/// One named member of a cohort and the request used to create its runtime instance.
/// </summary>
/// <typeparam name="TConfiguration">The caller-defined configuration snapshot type.</typeparam>
public sealed record AgentCohortMember<TConfiguration>
{
    /// <summary>
    /// Gets the unique member id within the cohort.
    /// </summary>
    public required string MemberId { get; init; }

    /// <summary>
    /// Gets the member role in the shared task.
    /// </summary>
    public required string Role { get; init; }

    /// <summary>
    /// Gets the runtime instance request for this member.
    /// </summary>
    public required AgentInstanceRequest<TConfiguration> InstanceRequest { get; init; }

    /// <summary>
    /// Gets optional input to use instead of the cohort shared task input.
    /// </summary>
    public string? InputOverride { get; init; }

    /// <summary>
    /// Gets non-secret member metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Validates the member shape.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(MemberId))
        {
            throw new ArgumentException("Cohort member id is required.", nameof(MemberId));
        }

        if (string.IsNullOrWhiteSpace(Role))
        {
            throw new ArgumentException("Cohort member role is required.", nameof(Role));
        }

        ArgumentNullException.ThrowIfNull(InstanceRequest);
        InstanceRequest.Validate();
    }
}
