namespace Hexalith.FrontComposer.Cli;

internal sealed record ProjectDocumentSet(string ProjectDirectory, IReadOnlyList<ProjectDocument> Documents);
