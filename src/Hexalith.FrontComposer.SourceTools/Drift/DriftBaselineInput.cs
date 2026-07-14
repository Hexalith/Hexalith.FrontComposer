using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Hexalith.FrontComposer.SourceTools.Drift;

internal sealed class DriftBaselineInput(string path, string text) : IEquatable<DriftBaselineInput> {
    internal string Path { get; } = path;
    internal string Text { get; } = text;

    internal static bool IsCandidate(string path) {
        // Story 9-1 P5 (T5): enforce the documented baseline naming contract. Previously every
        // *.json AdditionalText was treated as a candidate baseline, so an unrelated config file
        // would produce HFC1060/HFC1064 errors. Accepted prefixes mirror the schemaVersion
        // family ("frontcomposer.generated-ui-baseline*") and the historic short form
        // ("frontcomposer.drift-baseline*").
        string fileName = System.IO.Path.GetFileName(path);
        if (string.IsNullOrEmpty(fileName)) {
            return false;
        }

        if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        return fileName.StartsWith("frontcomposer.drift-baseline", StringComparison.OrdinalIgnoreCase)
            || fileName.StartsWith("frontcomposer.generated-ui-baseline", StringComparison.OrdinalIgnoreCase);
    }

    internal static DriftBaselineInput FromAdditionalText(AdditionalText text, CancellationToken cancellationToken) {
        SourceText? source = text.GetText(cancellationToken);
        return new DriftBaselineInput(text.Path, source?.ToString() ?? string.Empty);
    }

    public bool Equals(DriftBaselineInput? other)
        => other is not null && Path == other.Path && Text == other.Text;

    public override bool Equals(object? obj) => Equals(obj as DriftBaselineInput);

    public override int GetHashCode() {
        unchecked {
            return (StringComparer.Ordinal.GetHashCode(Path) * 397)
                ^ StringComparer.Ordinal.GetHashCode(Text);
        }
    }
}
