namespace DotNetAgents.ContextIntent;

/// <summary>
/// Controls how strictly the enforcement layer treats handoffs that lack a ContextIntent
/// envelope. The story 0813f0bc soft-rollout pattern: <see cref="WarnOnMissing"/> for the
/// first 30 days post-deployment, then promote to <see cref="RequireEmission"/> for hard
/// requirement. <see cref="Off"/> disables enforcement entirely (test scenarios only).
/// </summary>
public enum ContextIntentEnforcementMode
{
    /// <summary>No enforcement — handoffs with or without envelopes pass silently. Test/legacy only.</summary>
    Off = 0,

    /// <summary>Missing envelopes log a warning + emit a SDLC-friction event but do not block the handoff. Default for soft-rollout window.</summary>
    WarnOnMissing = 1,

    /// <summary>Missing envelopes block the handoff with an error. Production posture once rollout window is over.</summary>
    RequireEmission = 2,
}
