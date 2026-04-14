namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Display density level controlling spacing and sizing of rendered components.
/// </summary>
public enum DensityLevel {
    /// <summary>Reduced spacing for dense data displays.</summary>
    Compact,

    /// <summary>Default balanced spacing.</summary>
    Comfortable,

    /// <summary>Generous spacing for touch-friendly layouts.</summary>
    Roomy,
}
