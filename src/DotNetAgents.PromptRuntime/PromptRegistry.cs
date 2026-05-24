// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;

namespace DotNetAgents.PromptRuntime;

/// <summary>
/// In-process catalog of prompt registrations. Services add their entries at startup; the runtime
/// client consults the registry to obtain fallback bodies + metadata for keys it hasn't cached yet.
/// </summary>
public interface IPromptRegistry
{
    /// <summary>Register (or replace) a prompt entry for this host.</summary>
    void Register(PromptRegistration registration);

    /// <summary>Resolve a registration by key, or null if the service hasn't declared it.</summary>
    PromptRegistration? TryGet(string key);

    /// <summary>Resolve registrations associated with a chain contract reference.</summary>
    IReadOnlyCollection<PromptRegistration> FindByChainContractRef(string chainContractRef);

    /// <summary>Resolve registrations associated with a skill reference.</summary>
    IReadOnlyCollection<PromptRegistration> FindBySkillRef(string skillRef);

    /// <summary>All registrations for this host.</summary>
    IReadOnlyCollection<PromptRegistration> Entries { get; }
}

public sealed class PromptRegistry : IPromptRegistry
{
    private readonly ConcurrentDictionary<string, PromptRegistration> _entries = new(StringComparer.Ordinal);

    public void Register(PromptRegistration registration)
    {
        if (registration is null) throw new ArgumentNullException(nameof(registration));
        if (string.IsNullOrWhiteSpace(registration.Key))
            throw new ArgumentException("registration.Key is required.", nameof(registration));
        _entries[registration.Key] = registration;
    }

    public PromptRegistration? TryGet(string key) =>
        _entries.TryGetValue(key, out var r) ? r : null;

    public IReadOnlyCollection<PromptRegistration> FindByChainContractRef(string chainContractRef)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chainContractRef);

        return _entries.Values
            .Where(r => r.ChainContractRefs?.Contains(chainContractRef, StringComparer.Ordinal) == true)
            .ToArray();
    }

    public IReadOnlyCollection<PromptRegistration> FindBySkillRef(string skillRef)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillRef);

        return _entries.Values
            .Where(r => r.SkillRefs?.Contains(skillRef, StringComparer.Ordinal) == true)
            .ToArray();
    }

    public IReadOnlyCollection<PromptRegistration> Entries => _entries.Values.ToArray();
}
