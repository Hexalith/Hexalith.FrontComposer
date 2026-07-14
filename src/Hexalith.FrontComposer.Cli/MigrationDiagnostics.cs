using Microsoft.CodeAnalysis;

namespace Hexalith.FrontComposer.Cli;

internal static class MigrationDiagnostics {
#pragma warning disable RS2008 // The CLI reserves migration IDs in SourceTools AnalyzerReleases for Story 9-2 governance.
    public static readonly DiagnosticDescriptor ObsoleteDevOverlay = new(
        "HFCM9001",
        "Obsolete FrontComposer dev-mode registration",
        "Replace obsolete FrontComposer dev-mode registration",
        "HexalithFrontComposer.Migration",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "docs/migrations/9.1-to-9.2.md");

    public static readonly DiagnosticDescriptor ManualMigration = new(
        "HFCM9002",
        "Manual FrontComposer migration required",
        "Manual FrontComposer migration required",
        "HexalithFrontComposer.Migration",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "docs/migrations/9.1-to-9.2.md");
#pragma warning restore RS2008
}
