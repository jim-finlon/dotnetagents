// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;

namespace DotNetAgents.Security.Validation;

/// <summary>
/// Basic implementation of <see cref="ISanitizer"/> with common security checks.
/// </summary>
public class BasicSanitizer : ISanitizer
{
    private static readonly Regex EmailPattern = new(
        @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PhonePattern = new(
        @"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b|\b\(\d{3}\)\s?\d{3}[-.]?\d{4}\b",
        RegexOptions.Compiled);

    private static readonly Regex SsnPattern = new(
        @"\b\d{3}-?\d{2}-?\d{4}\b",
        RegexOptions.Compiled);

    private static readonly Regex CreditCardPattern = new(
        @"\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b",
        RegexOptions.Compiled);

    private static readonly string[] PromptInjectionKeywords = new[]
    {
        "ignore previous",
        "ignore all previous",
        "forget all previous",
        "disregard previous",
        "system:",
        "assistant:",
        "user:",
        "you are now",
        "you are a",
        "act as",
        "pretend to be",
        "simulate",
        "override",
        "bypass",
        "jailbreak"
    };

    /// <inheritdoc/>
    public string SanitizeInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input ?? string.Empty;

        // Remove null bytes
        var sanitized = input.Replace("\0", string.Empty);

        // Trim whitespace
        sanitized = sanitized.Trim();

        return sanitized;
    }

    /// <inheritdoc/>
    public string SanitizeOutput(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return output ?? string.Empty;

        // Remove null bytes
        var sanitized = output.Replace("\0", string.Empty);

        return sanitized;
    }

    /// <inheritdoc/>
    public bool ContainsSensitiveData(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return EmailPattern.IsMatch(text) ||
               PhonePattern.IsMatch(text) ||
               SsnPattern.IsMatch(text) ||
               CreditCardPattern.IsMatch(text);
    }

    /// <inheritdoc/>
    public bool DetectPromptInjection(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var lowerInput = input.ToLowerInvariant();

        return PromptInjectionKeywords.Any(keyword =>
            lowerInput.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public string MaskSensitiveData(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text ?? string.Empty;

        var masked = text;

        // Mask emails
        masked = EmailPattern.Replace(masked, m => MaskString(m.Value, 2, 2));

        // Mask phone numbers
        masked = PhonePattern.Replace(masked, m => MaskString(m.Value, 3, 4));

        // Mask SSNs
        masked = SsnPattern.Replace(masked, "***-**-****");

        // Mask credit cards
        masked = CreditCardPattern.Replace(masked, m => MaskString(m.Value, 4, 4));

        return masked;
    }

    private static string MaskString(string value, int prefixLength, int suffixLength)
    {
        if (value.Length <= prefixLength + suffixLength)
            return new string('*', value.Length);

        var prefix = value.Substring(0, prefixLength);
        var suffix = value.Substring(value.Length - suffixLength);
        var masked = new string('*', value.Length - prefixLength - suffixLength);

        return $"{prefix}{masked}{suffix}";
    }
}
