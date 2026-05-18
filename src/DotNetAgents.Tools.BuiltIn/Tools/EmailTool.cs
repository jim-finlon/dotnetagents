using System.Net;
using System.Net.Mail;
using System.Text.Json;
using DotNetAgents.Abstractions.Tools;

using DotNetAgents.Abstractions.Exceptions;

namespace DotNetAgents.Tools.BuiltIn;

/// <summary>
/// A tool for sending emails via SMTP.
/// </summary>
public class EmailTool : ITool
{
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly bool _useSsl;
    private readonly string? _username;
    private readonly string? _password;
    private static readonly JsonElement _inputSchema;

    static EmailTool()
    {
        _inputSchema = JsonSerializer.Deserialize<JsonElement>(@"{
            ""type"": ""object"",
            ""properties"": {
                ""to"": {
                    ""type"": ""string"",
                    ""description"": ""Recipient email address (comma-separated for multiple recipients)""
                },
                ""subject"": {
                    ""type"": ""string"",
                    ""description"": ""Email subject""
                },
                ""body"": {
                    ""type"": ""string"",
                    ""description"": ""Email body (plain text or HTML)""
                },
                ""from"": {
                    ""type"": ""string"",
                    ""description"": ""Sender email address (optional, uses configured default)""
                },
                ""cc"": {
                    ""type"": ""string"",
                    ""description"": ""CC email addresses (comma-separated)""
                },
                ""bcc"": {
                    ""type"": ""string"",
                    ""description"": ""BCC email addresses (comma-separated)""
                },
                ""is_html"": {
                    ""type"": ""boolean"",
                    ""description"": ""Whether the body is HTML. Default: false""
                }
            },
            ""required"": [""to"", ""subject"", ""body""]
        }");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailTool"/> class.
    /// </summary>
    /// <param name="smtpServer">SMTP server hostname.</param>
    /// <param name="smtpPort">SMTP server port. Default: 587.</param>
    /// <param name="useSsl">Whether to use SSL/TLS. Default: true.</param>
    /// <param name="username">SMTP username (optional).</param>
    /// <param name="password">SMTP password (optional).</param>
    public EmailTool(
        string smtpServer,
        int smtpPort = 587,
        bool useSsl = true,
        string? username = null,
        string? password = null)
    {
        _smtpServer = smtpServer ?? throw new ArgumentNullException(nameof(smtpServer));
        _smtpPort = smtpPort;
        _useSsl = useSsl;
        _username = username;
        _password = password;
    }

    /// <inheritdoc/>
    public string Name => "email";

    /// <inheritdoc/>
    public string Description => "Sends emails via SMTP. Supports multiple recipients, CC, BCC, and HTML email content.";

    /// <inheritdoc/>
    public JsonElement InputSchema => _inputSchema;

    /// <inheritdoc/>
    public async Task<ToolResult> ExecuteAsync(
        object input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var parameters = ParseInput(input);
        if (!parameters.TryGetValue("to", out var toObj) || toObj is not string to)
        {
            return ToolResult.Failure("Missing or invalid 'to' parameter.");
        }

        if (!parameters.TryGetValue("subject", out var subjectObj) || subjectObj is not string subject)
        {
            return ToolResult.Failure("Missing or invalid 'subject' parameter.");
        }

        if (!parameters.TryGetValue("body", out var bodyObj) || bodyObj is not string body)
        {
            return ToolResult.Failure("Missing or invalid 'body' parameter.");
        }

        var from = parameters.TryGetValue("from", out var fromObj) && fromObj is string fromStr ? fromStr : null;
        var cc = parameters.TryGetValue("cc", out var ccObj) && ccObj is string ccStr ? ccStr : null;
        var bcc = parameters.TryGetValue("bcc", out var bccObj) && bccObj is string bccStr ? bccStr : null;
        var isHtml = parameters.TryGetValue("is_html", out var htmlObj) &&
                     (htmlObj is bool htmlBool ? htmlBool : htmlObj is JsonElement je && je.ValueKind == JsonValueKind.True);

        try
        {
            using var message = new MailMessage();

            if (!string.IsNullOrWhiteSpace(from))
            {
                message.From = new MailAddress(from);
            }

            // Add recipients
            foreach (var recipient in to.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!string.IsNullOrWhiteSpace(recipient))
                {
                    message.To.Add(new MailAddress(recipient.Trim()));
                }
            }

            // Add CC
            if (!string.IsNullOrWhiteSpace(cc))
            {
                foreach (var ccRecipient in cc.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (!string.IsNullOrWhiteSpace(ccRecipient))
                    {
                        message.CC.Add(new MailAddress(ccRecipient.Trim()));
                    }
                }
            }

            // Add BCC
            if (!string.IsNullOrWhiteSpace(bcc))
            {
                foreach (var bccRecipient in bcc.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (!string.IsNullOrWhiteSpace(bccRecipient))
                    {
                        message.Bcc.Add(new MailAddress(bccRecipient.Trim()));
                    }
                }
            }

            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = isHtml;

            using var client = new SmtpClient(_smtpServer, _smtpPort)
            {
                EnableSsl = _useSsl
            };

            if (!string.IsNullOrWhiteSpace(_username) && !string.IsNullOrWhiteSpace(_password))
            {
                client.Credentials = new NetworkCredential(_username, _password);
            }

            await client.SendMailAsync(message, cancellationToken).ConfigureAwait(false);

            return ToolResult.Success(
                "Email sent successfully",
                new Dictionary<string, object>
                {
                    ["to"] = to,
                    ["subject"] = subject,
                    ["recipient_count"] = message.To.Count
                });
        }
        catch (SmtpException ex)
        {
            return ToolResult.Failure(
                $"SMTP error: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["to"] = to,
                    ["error_code"] = ex.StatusCode.ToString()
                });
        }
        catch (Exception ex)
        {
            return ToolResult.Failure(
                $"Failed to send email: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["to"] = to
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
}
