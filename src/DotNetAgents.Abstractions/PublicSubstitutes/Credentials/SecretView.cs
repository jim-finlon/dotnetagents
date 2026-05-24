// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Abstractions.PublicSubstitutes.Credentials;

/// <summary>
/// Non-string view of secret material owned by an <see cref="ICredentialAccessor"/>.
/// The owning accessor clears the backing memory on disposal.
/// </summary>
/// <param name="Value">
/// Secret characters. Treat this as borrowed memory and keep it inside the
/// lifetime of the accessor that returned it.
/// </param>
public readonly record struct SecretView(ReadOnlyMemory<char> Value)
{
    /// <summary>Number of characters currently exposed by this view.</summary>
    public int Length => Value.Length;

    /// <inheritdoc />
    public override string ToString() => $"SecretView(length={Length})";
}
