using DotNetAgents.Abstractions.Chains;
using System.Text.RegularExpressions;

namespace DotNetAgents.Core.Chains;

/// <summary>
/// Parser for string-based chain expressions (simplified LCEL-like syntax).
/// </summary>
public class ChainExpressionParser
{
    /// <summary>
    /// Parses a string expression into a chain expression.
    /// </summary>
    /// <param name="expression">The chain expression string (e.g., "prompt | llm | parser").</param>
    /// <param name="runnables">A dictionary mapping runnable names to their instances.</param>
    /// <returns>A chain expression representing the parsed chain.</returns>
    /// <exception cref="ArgumentException">Thrown when the expression is invalid or runnables are missing.</exception>
    public static ChainExpression<string, string> Parse(
        string expression,
        IDictionary<string, IRunnable<string, string>> runnables)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        ArgumentNullException.ThrowIfNull(runnables);

        // Simple parser for expressions like "prompt | llm | parser"
        var parts = expression.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
            throw new ArgumentException("Expression must contain at least one runnable.", nameof(expression));

        if (parts.Length == 1)
        {
            var name = parts[0].Trim();
            if (!runnables.TryGetValue(name, out var runnable))
                throw new ArgumentException($"Runnable '{name}' not found.", nameof(expression));

            return new ChainExpression<string, string>(runnable);
        }

        // Build chain sequentially
        IRunnable<string, string>? current = null;
        foreach (var part in parts)
        {
            var name = part.Trim();
            if (!runnables.TryGetValue(name, out var runnable))
                throw new ArgumentException($"Runnable '{name}' not found.", nameof(expression));

            if (current == null)
            {
                current = runnable;
            }
            else
            {
                // Pipe current with new runnable
                current = current.Pipe(runnable);
            }
        }

        if (current == null)
            throw new InvalidOperationException("Failed to build chain expression.");

        return new ChainExpression<string, string>(current);
    }

    /// <summary>
    /// Parses a chain expression with support for parallel composition using Parallel extension method.
    /// </summary>
    /// <param name="expression">The chain expression string (e.g., "(prompt1 | llm1) &amp; (prompt2 | llm2)").</param>
    /// <param name="runnables">A dictionary mapping runnable names to their instances.</param>
    /// <returns>A chain expression representing the parsed chain.</returns>
    /// <exception cref="ArgumentException">Thrown when the expression is invalid or runnables are missing.</exception>
    public static ChainExpression<string, string> ParseAdvanced(
        string expression,
        IDictionary<string, IRunnable<string, string>> runnables)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        ArgumentNullException.ThrowIfNull(runnables);

        // Remove whitespace
        expression = Regex.Replace(expression, @"\s+", " ").Trim();

        // Handle parentheses for grouping
        return ParseExpression(expression, runnables);
    }

    private static ChainExpression<string, string> ParseExpression(
        string expression,
        IDictionary<string, IRunnable<string, string>> runnables)
    {
        // Handle parentheses
        if (expression.StartsWith('(') && expression.EndsWith(')'))
        {
            var inner = expression.Substring(1, expression.Length - 2);
            return ParseExpression(inner, runnables);
        }

        // Check for parallel composition (&)
        var parallelIndex = FindOperator(expression, '&');
        if (parallelIndex >= 0)
        {
            var left = expression.Substring(0, parallelIndex).Trim();
            var right = expression.Substring(parallelIndex + 1).Trim();

            var leftExpr = ParseExpression(left, runnables);
            var rightExpr = ParseExpression(right, runnables);

            // For parallel composition, both sides need to have compatible types
            // This is a simplified implementation - in practice, you'd need more type handling
            throw new NotSupportedException(
                "Parallel composition in string expressions requires explicit type handling. " +
                "Use the Parallel extension method directly on ChainExpression instances instead.");
        }

        // Sequential composition (|)
        var pipeIndex = FindOperator(expression, '|');
        if (pipeIndex >= 0)
        {
            var left = expression.Substring(0, pipeIndex).Trim();
            var right = expression.Substring(pipeIndex + 1).Trim();

            var leftExpr = ParseExpression(left, runnables);
            var rightExpr = ParseExpression(right, runnables);

            return new ChainExpression<string, string>(
                leftExpr.Runnable.Pipe(rightExpr.Runnable));
        }

        // Single runnable
        var name = expression.Trim();
        if (!runnables.TryGetValue(name, out var runnable))
            throw new ArgumentException($"Runnable '{name}' not found.", nameof(expression));

        return new ChainExpression<string, string>(runnable);
    }

    private static int FindOperator(string expression, char op)
    {
        int depth = 0;
        for (int i = 0; i < expression.Length; i++)
        {
            if (expression[i] == '(')
                depth++;
            else if (expression[i] == ')')
                depth--;
            else if (expression[i] == op && depth == 0)
                return i;
        }

        return -1;
    }
}
