namespace Hexalith.FrontComposer.Cli;

internal static class MigrationCatalog {
    private static readonly MigrationEdge[] Edges = BuildEdges([
        new("9.1.0", "9.2.0", "docs/migrations/9.1-to-9.2.md"),
    ]);

    public static MigrationEdge? Resolve(string? from, string? to)
        => Edges.FirstOrDefault(edge => string.Equals(edge.FromVersion, from, StringComparison.Ordinal)
            && string.Equals(edge.ToVersion, to, StringComparison.Ordinal));

    private static MigrationEdge[] BuildEdges(MigrationEdge[] edges) {
        IGrouping<(string From, string To), MigrationEdge>[] duplicates = edges
            .GroupBy(edge => (edge.FromVersion, edge.ToVersion))
            .Where(g => g.Count() > 1)
            .ToArray();
        if (duplicates.Length > 0) {
            throw new InvalidOperationException(
                "Migration catalog contains duplicate edge(s): "
                    + string.Join(", ", duplicates.Select(g => g.Key.From + "->" + g.Key.To)));
        }

        return edges;
    }

    public static string UnsupportedMessage(string? from, string? to)
        => "Unsupported FrontComposer migration edge '"
            + OutputSanitizer.Sanitize(from)
            + "' -> '"
            + OutputSanitizer.Sanitize(to)
            + "'. Supported edges: "
            + string.Join(", ", Edges.Select(edge => edge.FromVersion + " -> " + edge.ToVersion))
            + ". DocsLink: docs/migrations/index.md.";
}
