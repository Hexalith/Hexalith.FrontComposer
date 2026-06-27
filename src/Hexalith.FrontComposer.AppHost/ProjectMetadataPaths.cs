namespace Hexalith.FrontComposer.AppHost;

/// <summary>
/// Resolves cross-repository project paths for the AppHost's <see cref="IProjectMetadata"/> locators.
/// All hosted services (EventStore, Tenants, Tenants UI) live in root-declared submodules under the
/// FrontComposer repository's references directory, so paths are resolved relative to that root rather
/// than via solution-local <c>ProjectReference</c>s (which would force the cross-repo projects to build
/// as part of this AppHost).
/// </summary>
internal static class ProjectMetadataPaths {
    public static string GetProjectPath(params string[] path)
        => Path.Combine(GetRepositoryRoot(), "references", Path.Combine(path));

    private static string GetRepositoryRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
}
