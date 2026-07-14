namespace Hexalith.FrontComposer.Cli;

internal static class TypeMatcher {
    public static TypeMatchResult Filter(InspectReport report, string requestedType) {
        string sanitized = OutputSanitizer.Sanitize(requestedType);
        string[] known = report.Files
            .Select(x => x.RelatedType)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        string[] exact = known.Where(x => string.Equals(x, requestedType, StringComparison.Ordinal)).ToArray();
        string[] simple = exact.Length == 0
            ? known.Where(x => string.Equals(SimpleName(x), requestedType, StringComparison.Ordinal)).ToArray()
            : [];

        string[] matches = exact.Length > 0 ? exact : simple;
        if (matches.Length == 0) {
            string closest = string.Join(", ", known.OrderBy(x => Distance(x, requestedType)).ThenBy(x => x, StringComparer.Ordinal).Take(5).Select(x => OutputSanitizer.Sanitize(x)));
            return TypeMatchResult.Fail(
                $"Generated output for type '{sanitized}' was not found. Closest known generated type names: {closest}.",
                ExitCodes.InvalidArguments);
        }

        if (matches.Length > 1) {
            return TypeMatchResult.Fail(
                $"Type name '{sanitized}' is ambiguous. Use one of: {string.Join(", ", matches.Select(x => OutputSanitizer.Sanitize(x)))}.",
                ExitCodes.InvalidArguments);
        }

        string match = matches[0];
        InspectReport filtered = report with {
            Files = report.Files.Where(x => x.RelatedType == match || (x.RelatedType is null && x.Family is GeneratedSourceFamily.McpManifest or GeneratedSourceFamily.TemplateManifest)).ToArray(),
            Diagnostics = report.Diagnostics.Where(x => x.RelatedType == match || x.RelatedType is null).ToArray(),
        };
        return TypeMatchResult.Ok(filtered);
    }

    private static string SimpleName(string metadataName) {
        int index = metadataName.LastIndexOf('.');
        return index < 0 ? metadataName : metadataName[(index + 1)..];
    }

    private const int MaxDistanceInputLength = 256;

    private static int Distance(string left, string right) {
        // Bound closest-type suggestions so hostile generated metadata names cannot force
        // quadratic work across unbounded strings. The output remains a hint, not an exact scorer.
        ReadOnlySpan<char> l = left.AsSpan(0, Math.Min(left.Length, MaxDistanceInputLength));
        ReadOnlySpan<char> r = right.AsSpan(0, Math.Min(right.Length, MaxDistanceInputLength));
        int[,] costs = new int[l.Length + 1, r.Length + 1];
        for (int i = 0; i <= l.Length; i++) {
            costs[i, 0] = i;
        }

        for (int j = 0; j <= r.Length; j++) {
            costs[0, j] = j;
        }

        for (int i = 1; i <= l.Length; i++) {
            for (int j = 1; j <= r.Length; j++) {
                int substitution = l[i - 1] == r[j - 1] ? 0 : 1;
                costs[i, j] = Math.Min(
                    Math.Min(costs[i - 1, j] + 1, costs[i, j - 1] + 1),
                    costs[i - 1, j - 1] + substitution);
            }
        }

        return costs[l.Length, r.Length];
    }
}
