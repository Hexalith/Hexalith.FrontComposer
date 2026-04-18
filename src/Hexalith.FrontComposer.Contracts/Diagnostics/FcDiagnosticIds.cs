namespace Hexalith.FrontComposer.Contracts.Diagnostics;

/// <summary>
/// Runtime-log diagnostic IDs (HFC2xxx). Reserved in code so logging call sites can reference
/// them symbolically; no analyzer release entry needed per architecture.md §648 precedent.
/// </summary>
/// <remarks>
/// Story 2-4 reserves HFC2100–HFC2102 for <c>FcLifecycleWrapper</c>. Extend this file as
/// additional runtime-log codes are allocated in subsequent stories.
/// </remarks>
public static class FcDiagnosticIds {
    /// <summary>Wrapper received a transition for an unknown CorrelationId (subscribe-after-terminal-cleanup race).</summary>
    public const string HFC2100_UnknownCorrelationId = "HFC2100";

    /// <summary>Wrapper observed an idempotency-resolved transition (Story 2-5 will surface "already done" copy).</summary>
    public const string HFC2101_IdempotencyResolvedObserved = "HFC2101";

    /// <summary>Wrapper threshold timer fired outside the UI-thread context.</summary>
    public const string HFC2102_ThresholdTimerOffUiThread = "HFC2102";

    /// <summary>
    /// Form abandonment guard suppressed its warning bar because the lifecycle state
    /// is Submitting (or the wrapper flagged its own Start-over navigation). Story 2-5 D13.
    /// Information severity — this is expected suppression, not anomaly.
    /// </summary>
    public const string HFC2103_AbandonmentDuringSubmitting = "HFC2103";

    /// <summary>
    /// Wrapper rendered the idempotent Info bar (Story 2-5 D3 / AC2). Logged so adopters
    /// can measure frequency and calibrate copy; CorrelationId is redacted to its first 8
    /// characters + ellipsis (Story 2-4 RT-4 — not a cryptographic hash).
    /// </summary>
    public const string HFC2104_IdempotentInfoBarRendered = "HFC2104";
}
