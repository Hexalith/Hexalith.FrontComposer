namespace Hexalith.FrontComposer.Contracts.Diagnostics;

/// <summary>
/// Formats customization diagnostics using the canonical teaching-message shape.
/// </summary>
public static class CustomizationDiagnosticFormatter {
    /// <summary>
    /// Formats the diagnostic as SourceTools/Shell-compatible teaching text.
    /// </summary>
    public static string Format(CustomizationDiagnostic diagnostic) {
        if (diagnostic is null) {
            throw new ArgumentNullException(nameof(diagnostic));
        }

        List<string> sections = [
            $"What: {diagnostic.What}",
            $"Expected: {diagnostic.Expected}",
            $"Got: {diagnostic.Got}",
            $"Fix: {diagnostic.Fix}",
        ];

        if (!string.IsNullOrWhiteSpace(diagnostic.Fallback)) {
            sections.Add($"Fallback: {diagnostic.Fallback}");
        }

        sections.Add($"DocsLink: {diagnostic.DocsLink}");
        return string.Join(Environment.NewLine, sections);
    }
}
