namespace Counter.Specimens.Domain;

/// <summary>
/// How long a purged specimen record stays recoverable.
/// </summary>
public enum PurgeRetention {
    /// <summary>The record stays recoverable for seven days.</summary>
    SevenDays,

    /// <summary>The record stays recoverable for thirty days.</summary>
    ThirtyDays,
}
