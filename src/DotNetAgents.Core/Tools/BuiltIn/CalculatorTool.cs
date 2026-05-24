// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using DotNetAgents.Abstractions.Tools;
using DotNetAgents.Abstractions.Exceptions;

namespace DotNetAgents.Core.Tools.BuiltIn;

/// <summary>
/// A calculator tool that can evaluate mathematical expressions.
/// </summary>
public class CalculatorTool : ITool
{
    private static readonly JsonElement _inputSchema = JsonSerializer.Deserialize<JsonElement>(@"{
            ""type"": ""object"",
            ""properties"": {
                ""expression"": {
                    ""type"": ""string"",
                    ""description"": ""The mathematical expression to evaluate (e.g., '2 + 2', 'sqrt(16)', 'pow(2, 3)')""
                }
            },
            ""required"": [""expression""]
        }");

    /// <inheritdoc/>
    public string Name => "calculator";

    /// <inheritdoc/>
    public string Description => "Evaluates mathematical expressions. Supports basic arithmetic operations (+, -, *, /), parentheses, and common functions like sqrt, pow, sin, cos, tan, log, ln.";

    /// <inheritdoc/>
    public JsonElement InputSchema => _inputSchema;

    /// <inheritdoc/>
    public async Task<ToolResult> ExecuteAsync(
        object input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var parameters = ParseInput(input);
        if (!parameters.TryGetValue("expression", out var expressionObj) || expressionObj is not string expression)
        {
            return ToolResult.Failure("Missing or invalid 'expression' parameter.");
        }

        if (string.IsNullOrWhiteSpace(expression))
        {
            return ToolResult.Failure("Expression cannot be null or empty.");
        }

        try
        {
            var result = EvaluateExpression(expression);
            return ToolResult.Success(
                result.ToString("G15", CultureInfo.InvariantCulture), // General format with up to 15 significant digits
                new Dictionary<string, object>
                {
                    ["expression"] = expression,
                    ["result"] = result
                });
        }
        catch (Exception ex)
        {
            return ToolResult.Failure(
                $"Failed to evaluate expression: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["expression"] = expression
                });
        }
    }

    private static IDictionary<string, object> ParseInput(object input)
    {
        if (input is JsonElement jsonElement)
        {
            var dict = new Dictionary<string, object>();
            foreach (var prop in jsonElement.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString()!,
                    JsonValueKind.Number => prop.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => prop.Value.ToString()
                };
            }
            return dict;
        }

        if (input is IDictionary<string, object> dictInput)
        {
            return dictInput;
        }

        throw new ArgumentException("Input must be JsonElement or IDictionary<string, object>", nameof(input));
    }

    private static double EvaluateExpression(string expression)
    {
        // Sanitize input - remove whitespace
        expression = Regex.Replace(expression, @"\s+", "");

        // Validate expression contains only allowed characters
        if (!Regex.IsMatch(expression, @"^[0-9+\-*/().,sqrtpowloglnsincostan]+$", RegexOptions.IgnoreCase))
        {
            throw new ArgumentException("Expression contains invalid characters.");
        }

        // Replace common functions with their equivalents
        expression = ReplaceFunctions(expression);

        // Use DataTable.Compute for safe evaluation
        // This is a simple approach; for production, consider using a proper expression parser
        try
        {
            var dataTable = new System.Data.DataTable();
            var result = dataTable.Compute(expression, null);

            if (result == null || result == DBNull.Value)
            {
                throw new InvalidOperationException("Expression evaluation returned null.");
            }

            return Convert.ToDouble(result);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Invalid mathematical expression: {ex.Message}", ex);
        }
    }

    private static string ReplaceFunctions(string expression)
    {
        // Replace sqrt(x) with Math.Sqrt(x)
        expression = Regex.Replace(expression, @"sqrt\(([^)]+)\)", match =>
        {
            var value = EvaluateExpression(match.Groups[1].Value);
            return Math.Sqrt(value).ToString("G15", System.Globalization.CultureInfo.InvariantCulture);
        }, RegexOptions.IgnoreCase);

        // Replace pow(x, y) with Math.Pow(x, y)
        expression = Regex.Replace(expression, @"pow\(([^,]+),([^)]+)\)", match =>
        {
            var x = EvaluateExpression(match.Groups[1].Value);
            var y = EvaluateExpression(match.Groups[2].Value);
            return Math.Pow(x, y).ToString("G15", System.Globalization.CultureInfo.InvariantCulture);
        }, RegexOptions.IgnoreCase);

        // Replace sin(x) with Math.Sin(x)
        expression = Regex.Replace(expression, @"sin\(([^)]+)\)", match =>
        {
            var value = EvaluateExpression(match.Groups[1].Value);
            return Math.Sin(value).ToString("G15", System.Globalization.CultureInfo.InvariantCulture);
        }, RegexOptions.IgnoreCase);

        // Replace cos(x) with Math.Cos(x)
        expression = Regex.Replace(expression, @"cos\(([^)]+)\)", match =>
        {
            var value = EvaluateExpression(match.Groups[1].Value);
            return Math.Cos(value).ToString("G15", System.Globalization.CultureInfo.InvariantCulture);
        }, RegexOptions.IgnoreCase);

        // Replace tan(x) with Math.Tan(x)
        expression = Regex.Replace(expression, @"tan\(([^)]+)\)", match =>
        {
            var value = EvaluateExpression(match.Groups[1].Value);
            return Math.Tan(value).ToString("G15", System.Globalization.CultureInfo.InvariantCulture);
        }, RegexOptions.IgnoreCase);

        // Replace log(x) with Math.Log10(x)
        expression = Regex.Replace(expression, @"log\(([^)]+)\)", match =>
        {
            var value = EvaluateExpression(match.Groups[1].Value);
            return Math.Log10(value).ToString("G15", System.Globalization.CultureInfo.InvariantCulture);
        }, RegexOptions.IgnoreCase);

        // Replace ln(x) with Math.Log(x)
        expression = Regex.Replace(expression, @"ln\(([^)]+)\)", match =>
        {
            var value = EvaluateExpression(match.Groups[1].Value);
            return Math.Log(value).ToString("G15", System.Globalization.CultureInfo.InvariantCulture);
        }, RegexOptions.IgnoreCase);

        return expression;
    }
}
