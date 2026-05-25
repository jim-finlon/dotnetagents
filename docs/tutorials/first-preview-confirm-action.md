# Tutorial: First Governed Preview/Confirm Action

Preview/confirm is the safest way to let an agent propose high-impact work
without immediately performing it.

## Why Build This

Agent mistakes become expensive when tools send messages, create records,
charge money, delete data, or publish content. Preview/confirm keeps authority
with the application and the operator.

## Define A Preview

```csharp
public sealed record SendReplyPreview(
    string TicketId,
    string Recipient,
    string Subject,
    string Body,
    string[] Warnings,
    string ConfirmationToken);
```

The confirmation token should identify the exact preview. In a real system,
store the preview server-side and expire it.

## Generate The Preview

```csharp
public sealed class ReplyPreviewService
{
    public SendReplyPreview Preview(string ticketId, string recipient, string body)
    {
        var warnings = new List<string>();

        if (!recipient.EndsWith("@example.com", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Recipient is outside the example.com domain.");
        }

        return new SendReplyPreview(
            ticketId,
            recipient,
            $"Re: Ticket {ticketId}",
            body,
            warnings.ToArray(),
            Convert.ToHexString(Guid.NewGuid().ToByteArray()));
    }
}
```

## Confirm The Action

```csharp
public sealed record ConfirmSendReplyRequest(
    string ConfirmationToken,
    string ApprovedBy);

public sealed record SendReplyResult(
    string TicketId,
    string Status,
    string AuditRef);
```

The confirm path should:

1. load the stored preview
2. verify the token is valid and unexpired
3. verify the approver is allowed
4. perform the send
5. record an audit reference
6. return a structured result

## MCP Tool Shape

Expose two tools:

- `tickets.reply.preview`
- `tickets.reply.confirm`

Do not expose a single `tickets.reply.send` tool that both drafts and sends
without a separate approval step.

## Validation Checklist

- preview does not mutate external state
- token cannot be reused after confirmation
- expired token fails closed
- confirm path records approver and audit ref
- warnings are visible before confirmation
- logs redact message body if it may contain private data

## Production Notes

The public packages provide the building blocks. Premium packages can add
managed evidence, approval dashboards, certification receipts, and enterprise
policy bundles, but the public design pattern is the same: preview first,
confirm explicitly, record the result.
