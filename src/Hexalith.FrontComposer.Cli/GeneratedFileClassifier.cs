namespace Hexalith.FrontComposer.Cli;

internal static class GeneratedFileClassifier {
    private static readonly (string Suffix, GeneratedSourceFamily Family)[] Suffixes = [
        (".CommandForm.g.razor.cs", GeneratedSourceFamily.CommandForm),
        ("CommandForm.g.razor.cs", GeneratedSourceFamily.CommandForm),
        (".CommandRenderer.g.razor.cs", GeneratedSourceFamily.CommandRenderer),
        ("Renderer.g.razor.cs", GeneratedSourceFamily.CommandRenderer),
        ("LifecycleBridge.g.cs", GeneratedSourceFamily.CommandLifecycleBridge),
        ("LastUsedSubscriber.g.cs", GeneratedSourceFamily.CommandLastUsedSubscriber),
        ("Page.g.razor.cs", GeneratedSourceFamily.CommandPage),
        ("LifecycleFeature.g.cs", GeneratedSourceFamily.FluxorFeature),
        ("Feature.g.cs", GeneratedSourceFamily.FluxorFeature),
        ("Actions.g.cs", GeneratedSourceFamily.FluxorActions),
        ("Reducers.g.cs", GeneratedSourceFamily.FluxorReducers),
        ("Registration.g.cs", GeneratedSourceFamily.Registration),
        (".g.razor.cs", GeneratedSourceFamily.ProjectionRazor),
    ];

    public static GeneratedFileInfo Classify(string projectDirectory, string path, bool absolutePaths = false) {
        string fileName = Path.GetFileName(path);
        if (fileName == "FrontComposerMcpManifest.g.cs") {
            return New(path, projectDirectory, fileName, GeneratedSourceFamily.McpManifest, null, absolutePaths);
        }

        if (fileName == "__FrontComposerProjectionTemplatesRegistration.g.cs") {
            return New(path, projectDirectory, fileName, GeneratedSourceFamily.TemplateManifest, null, absolutePaths);
        }

        foreach ((string suffix, GeneratedSourceFamily family) in Suffixes) {
            if (fileName.EndsWith(suffix, StringComparison.Ordinal)) {
                string related = fileName[..^suffix.Length];
                related = related.TrimEnd('.');
                if (related.EndsWith(".Command", StringComparison.Ordinal)) {
                    related = related[..^".Command".Length];
                }

                return New(path, projectDirectory, fileName, family, string.IsNullOrWhiteSpace(related) ? null : related, absolutePaths);
            }
        }

        return New(path, projectDirectory, fileName, GeneratedSourceFamily.Unknown, null, absolutePaths);
    }

    private static GeneratedFileInfo New(string path, string projectDirectory, string fileName, GeneratedSourceFamily family, string? relatedType, bool absolutePaths)
        => new(absolutePaths ? Path.GetFullPath(path) : PathUtilities.ToProjectRelative(projectDirectory, path), fileName, family, relatedType);
}
