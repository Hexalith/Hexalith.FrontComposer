namespace Hexalith.FrontComposer.Contracts.Diagnostics;

/// <summary>
/// Phase where a customization diagnostic was produced.
/// </summary>
public enum CustomizationDiagnosticPhase {
    /// <summary>Diagnostic produced during SourceTools build-time analysis or generation.</summary>
    Build = 0,

    /// <summary>Diagnostic produced while classifying hot-reload or rebuild requirements.</summary>
    HotReload = 1,

    /// <summary>Diagnostic produced by the Shell at runtime.</summary>
    Runtime = 2,
}
