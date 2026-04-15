namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>
/// Configuration for <see cref="StubCommandService"/>. Registered via
/// <c>IOptionsSnapshot&lt;StubCommandServiceOptions&gt;</c> at <c>Scoped</c> lifetime.
/// </summary>
/// <remarks>
/// Properties are mutable (not <c>init</c>-only) because <c>services.Configure&lt;T&gt;</c>
/// binds via an <see cref="System.Action{T}"/> that mutates a default instance.
/// </remarks>
public sealed class StubCommandServiceOptions {
    /// <summary>Gets or sets the delay before the stub returns an acknowledgement (emulates HTTP round-trip).</summary>
    public int AcknowledgeDelayMs { get; set; } = 100;

    /// <summary>Gets or sets the delay before the Syncing callback fires (emulates SignalR catch-up latency).</summary>
    public int SyncingDelayMs { get; set; } = 100;

    /// <summary>Gets or sets the delay before the Confirmed callback fires (emulates projection arrival).</summary>
    public int ConfirmDelayMs { get; set; } = 200;

    /// <summary>Gets or sets a value indicating whether to simulate a rejection instead of acknowledging.</summary>
    public bool SimulateRejection { get; set; }

    /// <summary>Gets or sets the reason string used when <see cref="SimulateRejection"/> is <see langword="true"/>.</summary>
    public string? RejectionReason { get; set; }

    /// <summary>Gets or sets the resolution guidance used when <see cref="SimulateRejection"/> is <see langword="true"/>.</summary>
    public string? RejectionResolution { get; set; }
}
