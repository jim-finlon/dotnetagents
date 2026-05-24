// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Abstractions.PublicSubstitutes.Credentials;

/// <summary>
/// Public-safe pointer to credential material. The reference names where a secret
/// lives; it never contains the secret value itself.
/// </summary>
/// <param name="Category">Credential category, for example <c>llm/openai</c>.</param>
/// <param name="Name">Credential name within the category, for example <c>api_key</c>.</param>
/// <param name="Version">Optional version label. Local adapters use <c>default</c> when omitted.</param>
public sealed record CredentialReference(string Category, string Name, string? Version = null);
