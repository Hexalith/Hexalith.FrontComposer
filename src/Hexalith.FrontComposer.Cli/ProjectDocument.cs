namespace Hexalith.FrontComposer.Cli;

internal sealed record ProjectDocument(string FullPath, string RelativePath, SourceFileContent? Content = null);
