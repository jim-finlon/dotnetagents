// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace DotNetAgents.Gateway;

/// <summary>
/// Stable cross-channel session identity for gateway-delivered agent work.
/// The normalized key intentionally carries agent, platform, chat, and user
/// dimensions so continuity can be scoped without merging unrelated operators.
/// </summary>
public sealed record GatewaySessionKey(
    string AgentId,
    string Platform,
    string ChatType,
    string ChatId,
    string UserId)
{
    public string StableKey =>
        string.Join("|",
            Normalize(AgentId),
            Normalize(Platform),
            Normalize(ChatType),
            Normalize(ChatId),
            Normalize(UserId));

    public static GatewaySessionKey Create(
        string agentId,
        string platform,
        string chatType,
        string chatId,
        string userId)
    {
        return new GatewaySessionKey(
            Require(agentId, nameof(agentId)),
            Require(platform, nameof(platform)),
            Require(chatType, nameof(chatType)),
            Require(chatId, nameof(chatId)),
            Require(userId, nameof(userId)));
    }

    private static string Require(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Gateway session key fields must be non-empty.", name);

        return value.Trim();
    }

    private static string Normalize(string value) =>
        value.Trim().ToLowerInvariant().Replace("|", "%7c", StringComparison.Ordinal);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GatewayChatType
{
    Direct = 0,
    Group = 1,
    Channel = 2,
    Thread = 3,
    Unknown = 4,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GatewayMessageDirection
{
    Inbound = 0,
    Outbound = 1,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GatewaySessionAuthState
{
    Unknown = 0,
    Allowed = 1,
    Denied = 2,
    Blocked = 3,
    RequiresPairing = 4,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GatewaySessionActiveState
{
    Idle = 0,
    Queued = 1,
    Running = 2,
    InterruptRequested = 3,
    Stopped = 4,
    Failed = 5,
}

public sealed record GatewaySession(
    GatewaySessionKey Key,
    string ConversationId,
    IReadOnlyList<string> CapabilityTags,
    GatewaySessionAuthState AuthState,
    GatewaySessionActiveState ActiveState,
    string? HomeWorkspace,
    DateTimeOffset UpdatedAtUtc);

public sealed record GatewayMessage(
    string MessageId,
    GatewaySessionKey SessionKey,
    GatewayMessageDirection Direction,
    string Body,
    IReadOnlyList<string> Attachments,
    DateTimeOffset ObservedAtUtc,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record GatewayDeliveryTarget(
    GatewaySessionKey SessionKey,
    string Platform,
    string RecipientId,
    string? ReplyToMessageId = null,
    IReadOnlyList<string>? RequiredCapabilityTags = null,
    IReadOnlyDictionary<string, string>? Metadata = null);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GatewayDeliveryStatus
{
    Accepted = 0,
    Delivered = 1,
    Failed = 2,
    Denied = 3,
}

public sealed record GatewayDeliveryReceipt(
    string ReceiptId,
    GatewayDeliveryTarget Target,
    GatewayDeliveryStatus Status,
    DateTimeOffset AttemptedAtUtc,
    string? ProviderMessageId = null,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public sealed record GatewaySessionAuthorizationContext(
    GatewaySessionKey SessionKey,
    string Platform,
    string UserId,
    string ChatId,
    string? HomeWorkspace,
    IReadOnlyList<string> CapabilityTags,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record GatewaySessionAuthorizationDecision(
    bool Allowed,
    GatewaySessionAuthState AuthState,
    string? Reason = null,
    string? ErrorCode = null)
{
    public static GatewaySessionAuthorizationDecision Allow(string? reason = null) =>
        new(true, GatewaySessionAuthState.Allowed, reason);

    public static GatewaySessionAuthorizationDecision Deny(string errorCode, string reason) =>
        new(false, GatewaySessionAuthState.Denied, reason, errorCode);
}

public interface IGatewaySessionAuthorizationPolicy
{
    Task<GatewaySessionAuthorizationDecision> AuthorizeAsync(
        GatewaySessionAuthorizationContext context,
        CancellationToken cancellationToken = default);
}

public interface IGatewayDeliveryRouter
{
    Task<GatewayDeliveryReceipt> DeliverAsync(
        GatewayDeliveryTarget target,
        GatewayMessage message,
        CancellationToken cancellationToken = default);
}

public static class GatewaySessionGuard
{
    public static GatewaySessionActiveState RequestInterrupt(GatewaySessionActiveState current) =>
        current is GatewaySessionActiveState.Running or GatewaySessionActiveState.Queued
            ? GatewaySessionActiveState.InterruptRequested
            : current;

    public static bool CanTransition(
        GatewaySessionActiveState current,
        GatewaySessionActiveState next)
    {
        if (current == next)
            return true;

        return current switch
        {
            GatewaySessionActiveState.Idle => next is GatewaySessionActiveState.Queued or GatewaySessionActiveState.Running,
            GatewaySessionActiveState.Queued => next is GatewaySessionActiveState.Running
                or GatewaySessionActiveState.InterruptRequested
                or GatewaySessionActiveState.Stopped
                or GatewaySessionActiveState.Failed,
            GatewaySessionActiveState.Running => next is GatewaySessionActiveState.InterruptRequested
                or GatewaySessionActiveState.Stopped
                or GatewaySessionActiveState.Failed
                or GatewaySessionActiveState.Idle,
            GatewaySessionActiveState.InterruptRequested => next is GatewaySessionActiveState.Stopped
                or GatewaySessionActiveState.Failed
                or GatewaySessionActiveState.Idle,
            GatewaySessionActiveState.Stopped => next is GatewaySessionActiveState.Idle
                or GatewaySessionActiveState.Queued,
            GatewaySessionActiveState.Failed => next is GatewaySessionActiveState.Idle
                or GatewaySessionActiveState.Queued,
            _ => false,
        };
    }

    public static GatewaySessionActiveState Transition(
        GatewaySessionActiveState current,
        GatewaySessionActiveState next)
    {
        if (!CanTransition(current, next))
            throw new InvalidOperationException($"Gateway session cannot transition from {current} to {next}.");

        return next;
    }
}
