// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Abstractions.PublicSubstitutes.Identity;

/// <summary>
/// Provides the public-safe identity of the current agent. This surface
/// deliberately excludes trust scores, signing keys, provenance, and leases.
/// </summary>
public interface IAgentIdentityDescriptor
{
    /// <summary>Gets the cached public-safe identity for the current agent.</summary>
    AgentIdentity Current { get; }
}
