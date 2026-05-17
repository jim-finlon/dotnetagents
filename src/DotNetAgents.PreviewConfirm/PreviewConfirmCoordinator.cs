using System.Security.Cryptography;
using System.Text;

namespace DotNetAgents.PreviewConfirm;

/// <summary>Coordinates preview → confirm for mutations that must not run without an explicit second step.</summary>
public sealed class PreviewConfirmCoordinator(IPreviewConfirmSessionStore store, TimeSpan defaultTimeToLive)
{
    private readonly IPreviewConfirmSessionStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly TimeSpan _defaultTimeToLive = defaultTimeToLive > TimeSpan.Zero
        ? defaultTimeToLive
        : throw new ArgumentOutOfRangeException(nameof(defaultTimeToLive));

    public TimeSpan DefaultTimeToLive => _defaultTimeToLive;

    /// <summary>Start a preview: returns session id, single-use token, and expiry.</summary>
    public async Task<PreviewConfirmStartResult> StartPreviewAsync(
        string previewPayload,
        TimeSpan? timeToLive = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(previewPayload);

        var sessionId = Guid.NewGuid();
        var token = PreviewConfirmToken.Create();
        var ttl = timeToLive ?? _defaultTimeToLive;
        var now = DateTimeOffset.UtcNow;
        var session = new PreviewConfirmSession(
            sessionId,
            token,
            previewPayload.Trim(),
            PreviewConfirmSessionState.AwaitingConfirmation,
            now,
            now.Add(ttl));

        await _store.SaveAsync(session, cancellationToken).ConfigureAwait(false);
        return new PreviewConfirmStartResult(sessionId, token, session.ExpiresAtUtc);
    }

    /// <summary>Validate token and mark session confirmed (idempotent if already confirmed for same token).</summary>
    public async Task<PreviewConfirmResult> ConfirmAsync(
        Guid sessionId,
        string confirmationToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(confirmationToken);

        var session = await _store.GetAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (session is null)
            return PreviewConfirmResult.NotFound(sessionId);

        if (session.State == PreviewConfirmSessionState.Confirmed)
            return TokensEqual(session.ConfirmationToken, confirmationToken)
                ? PreviewConfirmResult.Ok(session)
                : PreviewConfirmResult.TokenMismatch(sessionId);

        if (session.State != PreviewConfirmSessionState.AwaitingConfirmation)
            return PreviewConfirmResult.InvalidState(sessionId, session.State);

        if (DateTimeOffset.UtcNow > session.ExpiresAtUtc)
        {
            var expired = session with { State = PreviewConfirmSessionState.Expired };
            await _store.SaveAsync(expired, cancellationToken).ConfigureAwait(false);
            return PreviewConfirmResult.Expired(sessionId);
        }

        if (!TokensEqual(session.ConfirmationToken, confirmationToken))
            return PreviewConfirmResult.TokenMismatch(sessionId);

        var confirmed = session with { State = PreviewConfirmSessionState.Confirmed };
        await _store.SaveAsync(confirmed, cancellationToken).ConfigureAwait(false);
        return PreviewConfirmResult.Ok(confirmed);
    }

    /// <summary>Mark preview rejected (e.g. operator declined).</summary>
    public async Task<PreviewConfirmResult> RejectAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _store.GetAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (session is null)
            return PreviewConfirmResult.NotFound(sessionId);

        if (session.State != PreviewConfirmSessionState.AwaitingConfirmation)
            return PreviewConfirmResult.InvalidState(sessionId, session.State);

        var rejected = session with { State = PreviewConfirmSessionState.Rejected };
        await _store.SaveAsync(rejected, cancellationToken).ConfigureAwait(false);
        return PreviewConfirmResult.Ok(rejected);
    }

    private static bool TokensEqual(string a, string b)
    {
        var ab = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        if (ab.Length != bb.Length)
            return false;
        return CryptographicOperations.FixedTimeEquals(ab, bb);
    }
}

public readonly record struct PreviewConfirmStartResult(Guid SessionId, string ConfirmationToken, DateTimeOffset ExpiresAtUtc);

public sealed record PreviewConfirmResult
{
    public bool Success { get; private init; }
    public PreviewConfirmSession? Session { get; private init; }
    public PreviewConfirmFailureReason? Failure { get; private init; }
    public Guid? SessionId { get; private init; }
    public PreviewConfirmSessionState? ActualState { get; private init; }

    public static PreviewConfirmResult Ok(PreviewConfirmSession session) =>
        new() { Success = true, Session = session };

    public static PreviewConfirmResult NotFound(Guid sessionId) =>
        new() { Success = false, Failure = PreviewConfirmFailureReason.NotFound, SessionId = sessionId };

    public static PreviewConfirmResult Expired(Guid sessionId) =>
        new() { Success = false, Failure = PreviewConfirmFailureReason.Expired, SessionId = sessionId };

    public static PreviewConfirmResult TokenMismatch(Guid sessionId) =>
        new() { Success = false, Failure = PreviewConfirmFailureReason.TokenMismatch, SessionId = sessionId };

    public static PreviewConfirmResult InvalidState(Guid sessionId, PreviewConfirmSessionState state) =>
        new() { Success = false, Failure = PreviewConfirmFailureReason.InvalidState, SessionId = sessionId, ActualState = state };
}

public enum PreviewConfirmFailureReason
{
    NotFound,
    Expired,
    TokenMismatch,
    InvalidState
}
