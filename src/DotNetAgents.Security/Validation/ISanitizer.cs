// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Security.Validation;

/// <summary>
/// Interface for sanitizing and validating inputs and outputs.
/// </summary>
public interface ISanitizer
{
    /// <summary>
    /// Sanitizes input text to prevent injection attacks.
    /// </summary>
    /// <param name="input">The input text to sanitize.</param>
    /// <returns>The sanitized input.</returns>
    string SanitizeInput(string input);

    /// <summary>
    /// Sanitizes output text to prevent data leakage.
    /// </summary>
    /// <param name="output">The output text to sanitize.</param>
    /// <returns>The sanitized output.</returns>
    string SanitizeOutput(string output);

    /// <summary>
    /// Checks if the text contains sensitive data (PII).
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>True if sensitive data is detected; otherwise, false.</returns>
    bool ContainsSensitiveData(string text);

    /// <summary>
    /// Detects potential prompt injection attempts.
    /// </summary>
    /// <param name="input">The input text to check.</param>
    /// <returns>True if prompt injection is detected; otherwise, false.</returns>
    bool DetectPromptInjection(string input);

    /// <summary>
    /// Masks sensitive data in text.
    /// </summary>
    /// <param name="text">The text containing sensitive data.</param>
    /// <returns>The text with sensitive data masked.</returns>
    string MaskSensitiveData(string text);
}
