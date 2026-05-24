// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Abstractions.PublicSubstitutes.Lab;

/// <summary>
/// Public-safe advisory posture for the environment the agent is running in.
/// Values are descriptive only; sandbox enforcement remains the host's job.
/// </summary>
public sealed record LabEnvironment(
    string Kind,
    bool NetworkEgressAllowed,
    bool FileSystemWriteAllowed,
    string? EnvironmentRef = null);
