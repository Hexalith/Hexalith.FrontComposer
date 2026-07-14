namespace Hexalith.FrontComposer.Cli;

internal sealed record MigrationEntry(
    string DiagnosticId,
    string Kind,
    string Path,
    string What,
    string Expected,
    string Got,
    string Fix,
    string DocsLink,
    string? Diff,
    bool FormattingApplied = false);
