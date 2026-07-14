namespace Hexalith.FrontComposer.Cli;

internal sealed record InspectDiagnostic(
    string Id,
    string Severity,
    string? RelatedType,
    string? RelativePath,
    string What,
    string Expected,
    string Got,
    string Fix,
    string DocsLink);
