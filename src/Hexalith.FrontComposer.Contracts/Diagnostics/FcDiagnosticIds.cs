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
}
