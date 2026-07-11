namespace Hexalith.FrontComposer.Testing;

/// <summary>Deterministic outcomes supported by <see cref="TestCommandService"/>.</summary>
public enum TestCommandOutcome {
    /// <summary>The command is acknowledged, synced, and confirmed.</summary>
    Success,
    /// <summary>The command is rejected.</summary>
    Rejected,
    /// <summary>The command times out after reaching Syncing.</summary>
    Timeout,
    /// <summary>The command remains unresolved at Syncing.</summary>
    StallAtSyncing,
}
