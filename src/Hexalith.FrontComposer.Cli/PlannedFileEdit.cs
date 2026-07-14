namespace Hexalith.FrontComposer.Cli;

internal sealed record PlannedFileEdit(
    string FullPath,
    string CanonicalPath,
    string RelativePath,
    SourceFileContent OriginalContent,
    string UpdatedText,
    string OriginalHash,
    IReadOnlyList<MigrationEntry> Entries);
