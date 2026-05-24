// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.StructuredOutput;

/// <summary>
/// Model that can generate structured (typed) output, optionally with schema or constraints.
/// </summary>
/// <typeparam name="TInput">Input type (e.g. string prompt or chat messages).</typeparam>
public interface IStructuredModel<TInput>
{
    /// <summary>
    /// Generate output as type <typeparamref name="T"/>. Schema is optional when derivable from T.
    /// </summary>
    /// <param name="input">Input (e.g. prompt).</param>
    /// <param name="schema">Optional JSON schema for the provider; when null, may be derived from T.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Deserialized instance of T.</returns>
    Task<T> GenerateStructuredAsync<T>(TInput input, JsonSchemaDefinition? schema = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate output and validate with the given constraint. Retries on validation failure when configured.
    /// </summary>
    Task<T> GenerateConstrainedAsync<T>(TInput input, IConstraint<T> constraint, CancellationToken cancellationToken = default);
}
