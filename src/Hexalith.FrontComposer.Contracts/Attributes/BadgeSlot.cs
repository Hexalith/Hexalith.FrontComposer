namespace Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Defines semantic badge color slots for projection field values.
/// Maps to Fluent UI badge appearance tokens at render time.
/// </summary>
public enum BadgeSlot
{
    /// <summary>Default neutral badge appearance.</summary>
    Neutral,

    /// <summary>Informational badge appearance.</summary>
    Info,

    /// <summary>Success/positive badge appearance.</summary>
    Success,

    /// <summary>Warning badge appearance.</summary>
    Warning,

    /// <summary>Danger/error badge appearance.</summary>
    Danger,

    /// <summary>Accent/highlight badge appearance.</summary>
    Accent,
}
