// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Observability.Metrics;

/// <summary>
/// Provides pricing information for various LLM models.
/// </summary>
public static class ModelPricing
{
    private static readonly Dictionary<string, (decimal InputPricePer1K, decimal OutputPricePer1K)> Pricing = new()
    {
        // OpenAI Models (prices per 1K tokens as of 2024)
        ["gpt-4"] = (0.03m, 0.06m),
        ["gpt-4-turbo"] = (0.01m, 0.03m),
        ["gpt-4-turbo-preview"] = (0.01m, 0.03m),
        ["gpt-4-32k"] = (0.06m, 0.12m),
        ["gpt-3.5-turbo"] = (0.0005m, 0.0015m),
        ["gpt-3.5-turbo-16k"] = (0.003m, 0.004m),

        // Anthropic Claude Models
        ["claude-3-opus"] = (0.015m, 0.075m),
        ["claude-3-sonnet"] = (0.003m, 0.015m),
        ["claude-3-haiku"] = (0.00025m, 0.00125m),
        ["claude-2"] = (0.008m, 0.024m),
        ["claude-instant"] = (0.0008m, 0.0024m),

        // Azure OpenAI (same as OpenAI, but may have different model names)
        ["gpt-4o"] = (0.005m, 0.015m),
        ["gpt-4o-mini"] = (0.00015m, 0.0006m),
    };

    /// <summary>
    /// Gets the pricing for a model.
    /// </summary>
    /// <param name="model">The model name.</param>
    /// <returns>A tuple containing input price per 1K tokens and output price per 1K tokens, or null if not found.</returns>
    public static (decimal InputPricePer1K, decimal OutputPricePer1K)? GetPricing(string model)
    {
        if (string.IsNullOrWhiteSpace(model))
            return null;

        // Try exact match first
        if (Pricing.TryGetValue(model.ToLowerInvariant(), out var pricing))
        {
            return pricing;
        }

        // Try partial match (e.g., "gpt-4" matches "gpt-4-turbo")
        var modelLower = model.ToLowerInvariant();
        foreach (var kvp in Pricing)
        {
            if (modelLower.Contains(kvp.Key) || kvp.Key.Contains(modelLower))
            {
                return kvp.Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Calculates the cost for a given model and token counts.
    /// </summary>
    /// <param name="model">The model name.</param>
    /// <param name="inputTokens">The number of input tokens.</param>
    /// <param name="outputTokens">The number of output tokens.</param>
    /// <returns>The calculated cost in USD, or null if pricing is not available.</returns>
    public static decimal? CalculateCost(string model, int inputTokens, int outputTokens)
    {
        var pricing = GetPricing(model);
        if (pricing == null)
            return null;

        var inputCost = (inputTokens / 1000.0m) * pricing.Value.InputPricePer1K;
        var outputCost = (outputTokens / 1000.0m) * pricing.Value.OutputPricePer1K;
        return inputCost + outputCost;
    }

    /// <summary>
    /// Registers custom pricing for a model.
    /// </summary>
    /// <param name="model">The model name.</param>
    /// <param name="inputPricePer1K">The input price per 1K tokens.</param>
    /// <param name="outputPricePer1K">The output price per 1K tokens.</param>
    public static void RegisterPricing(string model, decimal inputPricePer1K, decimal outputPricePer1K)
    {
        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Model name cannot be null or whitespace.", nameof(model));

        Pricing[model.ToLowerInvariant()] = (inputPricePer1K, outputPricePer1K);
    }
}
