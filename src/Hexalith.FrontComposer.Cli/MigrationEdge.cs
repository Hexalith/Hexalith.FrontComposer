namespace Hexalith.FrontComposer.Cli;

internal sealed record MigrationEdge(string FromVersion, string ToVersion, string DocsLink);
