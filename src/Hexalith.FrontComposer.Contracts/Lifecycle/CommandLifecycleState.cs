namespace Hexalith.FrontComposer.Contracts.Lifecycle;

/// <summary>
/// Tracks the lifecycle state of a dispatched command from submission through confirmation.
/// </summary>
public enum CommandLifecycleState
{
    /// <summary>Default -- no command in flight.</summary>
    Idle,

    /// <summary>Command sent, awaiting acknowledgement.</summary>
    Submitting,

    /// <summary>EventStore accepted (202), awaiting projection sync.</summary>
    Acknowledged,

    /// <summary>Projection update detected, applying to UI.</summary>
    Syncing,

    /// <summary>Projection state confirmed in UI.</summary>
    Confirmed,

    /// <summary>Command rejected by domain logic.</summary>
    Rejected,
}
