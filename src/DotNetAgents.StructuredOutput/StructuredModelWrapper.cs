using System.Text.Json;
using DotNetAgents.Abstractions.Models;

namespace DotNetAgents.StructuredOutput;

/// <summary>
/// Wraps an <see cref="ILLMModel{TInput, TOutput}"/> (string → string) to implement <see cref="IStructuredModel{TInput}"/>.
/// Requests JSON from the LLM and deserializes to T. Schema can be passed to the underlying model by a provider-specific adapter.
/// </summary>
public sealed class StructuredModelWrapper : IStructuredModel<string>
{
    private readonly ILLMModel<string, string> _model;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly int _maxRetries;

    public StructuredModelWrapper(ILLMModel<string, string> model, JsonSerializerOptions? jsonOptions = null, int maxRetries = 1)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _jsonOptions = jsonOptions ?? new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        _maxRetries = maxRetries >= 0 ? maxRetries : 1;
    }

    /// <inheritdoc />
    public async Task<T> GenerateStructuredAsync<T>(string input, JsonSchemaDefinition? schema = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var effectiveSchema = schema ?? SchemaGenerator.FromType<T>();
        var prompt = BuildPrompt(input, effectiveSchema);
        var lastException = (Exception?)null;
        for (var attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                var response = await _model.GenerateAsync(prompt, null, cancellationToken).ConfigureAwait(false);
                var json = ExtractJson(response);
                var result = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                if (result == null)
                    throw new InvalidOperationException("Deserialization returned null.");
                return result;
            }
            catch (JsonException ex)
            {
                lastException = ex;
                if (attempt == _maxRetries) throw;
            }
        }
        throw lastException ?? new InvalidOperationException("GenerateStructuredAsync failed.");
    }

    /// <inheritdoc />
    public async Task<T> GenerateConstrainedAsync<T>(string input, IConstraint<T> constraint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(constraint);
        var schema = constraint.GetSchemaFragment() ?? SchemaGenerator.FromType<T>();
        for (var attempt = 0; attempt <= _maxRetries; attempt++)
        {
            var result = await GenerateStructuredAsync<T>(input, schema, cancellationToken).ConfigureAwait(false);
            var err = constraint.Validate(result);
            if (err == null)
                return result;
            if (attempt == _maxRetries)
                throw new InvalidOperationException($"Constraint validation failed: {err}");
        }
        throw new InvalidOperationException("GenerateConstrainedAsync failed.");
    }

    private static string BuildPrompt(string userPrompt, JsonSchemaDefinition schema)
    {
        return $@"{userPrompt}

Respond with a single JSON object that conforms to this schema. Return only the JSON, no markdown or explanation.
Schema type: {schema.Type}.";
    }

    private static string ExtractJson(string response)
    {
        var s = response.Trim();
        var start = s.IndexOf('{');
        var end = s.LastIndexOf('}');
        if (start >= 0 && end > start)
            return s.Substring(start, end - start + 1);
        return s;
    }
}
