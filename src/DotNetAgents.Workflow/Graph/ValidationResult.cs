namespace DotNetAgents.Workflow.Graph;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public record ValidationResult
{
    /// <summary>
    /// Gets whether the validation passed.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the validation error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets a list of validation errors (for multiple validation failures).
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with a single error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    public static ValidationResult Failure(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        return new()
        {
            IsValid = false,
            ErrorMessage = errorMessage,
            Errors = new[] { errorMessage }
        };
    }

    /// <summary>
    /// Creates a failed validation result with multiple error messages.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    public static ValidationResult Failure(IReadOnlyList<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.Count == 0)
        {
            throw new ArgumentException("Errors list cannot be empty.", nameof(errors));
        }

        return new()
        {
            IsValid = false,
            ErrorMessage = errors[0],
            Errors = errors
        };
    }
}
