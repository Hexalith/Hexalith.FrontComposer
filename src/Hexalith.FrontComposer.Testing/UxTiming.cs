namespace Hexalith.FrontComposer.Testing;

/// <summary>Canonical default UX budgets, independent of machine time and adopter overrides.</summary>
public static class UxTiming
{
    /// <summary>Trailing announcement window and post-accept transient retry delay.</summary>
    public static TimeSpan CoalescingWindow => TimeSpan.FromMilliseconds(250);
    /// <summary>One transient retry after acknowledgement, reusing the same MessageId.</summary>
    public static TimeSpan PostAcceptanceRetry => TimeSpan.FromMilliseconds(250);
    /// <summary>Slow-query threshold.</summary>
    public static TimeSpan SlowQuery => TimeSpan.FromMilliseconds(2_000);
    /// <summary>Recovery-notice lifetime.</summary>
    public static TimeSpan RecoveryNotice => TimeSpan.FromMilliseconds(3_000);
    /// <summary>Fresh-row cue lifetime.</summary>
    public static TimeSpan FreshRowLifetime => TimeSpan.FromMilliseconds(10_000);
    /// <summary>Lifecycle degradation threshold after acceptance.</summary>
    public static TimeSpan Degraded => TimeSpan.FromMilliseconds(10_000);
    /// <summary>Command-status polling cadence.</summary>
    public static TimeSpan StatusPolling => TimeSpan.FromMilliseconds(1_000);
    /// <summary>Local command-status polling ceiling.</summary>
    public static TimeSpan PollingExhaustion => TimeSpan.FromMilliseconds(120_000);
    /// <summary>Projection fallback polling cadence.</summary>
    public static TimeSpan ProjectionPolling => TimeSpan.FromMilliseconds(15_000);
    /// <summary>Maximum jittered projection retry delay.</summary>
    public static TimeSpan ProjectionRetryCap => TimeSpan.FromMilliseconds(30_000);
    /// <summary>Maximum closed-connection restart interval.</summary>
    public static TimeSpan ConnectionRestart => TimeSpan.FromMilliseconds(10_000);
    /// <summary>Unsaved-edit guard threshold.</summary>
    public static TimeSpan Abandonment => TimeSpan.FromSeconds(30);
}
