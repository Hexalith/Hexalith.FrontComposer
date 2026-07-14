namespace Hexalith.FrontComposer.Cli;

internal sealed record GeneratedFileInfo(
    string RelativePath,
    string FileName,
    GeneratedSourceFamily Family,
    string? RelatedType);
