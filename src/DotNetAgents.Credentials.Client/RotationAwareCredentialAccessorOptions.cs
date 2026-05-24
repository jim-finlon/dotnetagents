// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Credentials.Client;

/// <summary>
/// Configuration for <see cref="IRotationAwareCredentialAccessor"/>.
/// </summary>
public sealed class RotationAwareCredentialAccessorOptions
{
    /// <summary>
    /// Default cache lifetime. Short by design so rotations converge quickly
    /// even when an event channel is unavailable.
    /// </summary>
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromSeconds(60);
}
