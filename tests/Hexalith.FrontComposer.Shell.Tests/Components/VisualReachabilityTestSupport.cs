namespace Hexalith.FrontComposer.Shell.Tests.Components;

internal static class VisualReachabilityTestSupport {
    internal static string ReadShellComponentCss(params string[] segments) {
        string[] pathSegments = [
            RepositoryRoot(),
            "src",
            "Hexalith.FrontComposer.Shell",
            "Components",
            .. segments,
        ];

        return File.ReadAllText(Path.Combine(pathSegments));
    }

    private static string RepositoryRoot() {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null) {
            if (File.Exists(Path.Combine(dir.FullName, "Hexalith.FrontComposer.slnx"))) {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
