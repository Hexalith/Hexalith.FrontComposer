namespace Hexalith.FrontComposer.Contracts.Diagnostics;

/// <summary>
/// Dependency-light severity for shared customization diagnostics.
/// </summary>
public enum CustomizationDiagnosticSeverity {
    /// <summary>Informational diagnostic that does not require immediate action.</summary>
    Information = 0,

    /// <summary>Warning diagnostic for recoverable customization problems.</summary>
    Warning = 1,

    /// <summary>Error diagnostic for customization problems that suppress unsafe selection.</summary>
    Error = 2,
}
