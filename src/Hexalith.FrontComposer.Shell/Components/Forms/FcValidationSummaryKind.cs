namespace Hexalith.FrontComposer.Shell.Components.Forms;

/// <summary>Identifies the canonical focus-only validation outcome being presented.</summary>
public enum FcValidationSummaryKind {
    /// <summary>A client validation attempt was blocked before dispatch.</summary>
    ClientValidation,

    /// <summary>An authoritative rejection supplied support-safe mapped field errors.</summary>
    MappedServerRejection,
}
