// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Abstractions.PublicSubstitutes.Identity;

/// <summary>
/// Public-safe description of the running agent for logging, observability,
/// and friendly self-reference.
/// </summary>
/// <param name="ActorId">Stable actor id, for example <c>local-public-agent</c>.</param>
/// <param name="DisplayName">Human-readable display name.</param>
/// <param name="ActorType">Actor category, for example <c>WorkstationSession</c> or <c>AgentInstance</c>.</param>
/// <param name="Capability">Optional coarse capability label.</param>
public sealed record AgentIdentity(
    string ActorId,
    string DisplayName,
    string ActorType,
    string? Capability = null);
