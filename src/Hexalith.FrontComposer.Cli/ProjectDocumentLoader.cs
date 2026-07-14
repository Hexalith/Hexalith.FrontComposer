using System.Xml.Linq;

namespace Hexalith.FrontComposer.Cli;

internal static class ProjectDocumentLoader {
    public static bool HasTopLevelImports(string projectPath) {
        XDocument? project = TryLoadProject(projectPath);
        return project?.Root?.Elements().Any(x => string.Equals(x.Name.LocalName, "Import", StringComparison.Ordinal)) == true;
    }

    public static ProjectDocumentSet Load(string projectPath) {
        string projectFullPath = Path.GetFullPath(projectPath);
        string projectDirectory = Path.GetDirectoryName(projectFullPath)!;
        XDocument? project = TryLoadProject(projectFullPath);
        if (project is null) {
            return new ProjectDocumentSet(projectDirectory, []);
        }

        List<ProjectDocument> documents = [];
        List<(string Include, string Exclude, string? Link)> compileItems = project
            .Descendants()
            .Where(x => string.Equals(x.Name.LocalName, "Compile", StringComparison.Ordinal))
            .Select(x => (
                (string?)x.Attribute("Include") ?? string.Empty,
                (string?)x.Attribute("Exclude") ?? string.Empty,
                (string?)x.Attribute("Link")))
            .Where(x => !string.IsNullOrWhiteSpace(x.Item1))
            .ToList();

        if (compileItems.Count == 0) {
            documents.AddRange(EnumerateGlob(projectDirectory, "**/*.cs", "bin/**;obj/**", link: null));
        }
        else {
            foreach ((string include, string exclude, string? link) in compileItems) {
                documents.AddRange(EnumerateGlob(projectDirectory, include, exclude, link));
            }
        }

        return new ProjectDocumentSet(
            projectDirectory,
            documents
                .GroupBy(x => PathUtilities.Canonical(x.FullPath), PathUtilities.PathComparer)
                .Select(x => x.First())
                .OrderBy(x => x.RelativePath, StringComparer.Ordinal)
                .ToArray());
    }

    private static XDocument? TryLoadProject(string projectPath) {
        try {
            return XDocument.Load(projectPath);
        }
        catch (System.Xml.XmlException) {
            return null;
        }
        catch (IOException) {
            return null;
        }
    }

    private static IEnumerable<ProjectDocument> EnumerateGlob(string projectDirectory, string include, string exclude, string? link) {
        string[] excludes = exclude.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string projectRoot = PathUtilities.Canonical(projectDirectory);
        string projectRootWithSep = projectRoot.EndsWith(Path.DirectorySeparatorChar)
            ? projectRoot
            : projectRoot + Path.DirectorySeparatorChar;

        foreach (string pattern in include.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
            foreach (string path in Expand(projectDirectory, pattern)) {
                string canonical = PathUtilities.Canonical(path);
                bool insideProject = string.Equals(canonical, projectRoot, PathUtilities.PathComparison)
                    || canonical.StartsWith(projectRootWithSep, PathUtilities.PathComparison);

                if (!insideProject) {
                    // P-D5 / AC23: a Compile Include that resolves outside the project root is allowed only when the element
                    // declares a Link attribute. The link target is then treated as project-relative for reporting.
                    if (string.IsNullOrWhiteSpace(link)) {
                        continue;
                    }

                    string linkRelative = link.Replace('\\', '/').TrimStart('/');
                    // Reject `..` traversal and rooted Link values; the link must be a clean project-relative path.
                    if (Path.IsPathRooted(linkRelative) || linkRelative.Split('/').Any(s => s == "..")) {
                        continue;
                    }

                    yield return new ProjectDocument(path, linkRelative);
                    continue;
                }

                string relative = PathUtilities.ToProjectRelative(projectDirectory, path);
                if (relative == PathUtilities.RedactedPathSentinel || IsExcluded(relative, excludes)) {
                    continue;
                }

                yield return new ProjectDocument(path, relative);
            }
        }
    }

    private static IEnumerable<string> Expand(string projectDirectory, string pattern) {
        string normalized = pattern.Replace('\\', '/');
        if (!normalized.Contains('*', StringComparison.Ordinal)) {
            string path;
            try {
                path = Path.GetFullPath(normalized.Replace('/', Path.DirectorySeparatorChar), projectDirectory);
            }
            catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException) {
                return [];
            }

            return File.Exists(path) ? [path] : [];
        }

        if (normalized is "**/*.cs" or "**.cs") {
            return Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories);
        }

        string filePattern = Path.GetFileName(normalized);
        string directoryPart = normalized[..^filePattern.Length].TrimEnd('/');
        SearchOption search = directoryPart.EndsWith("**", StringComparison.Ordinal) || normalized.Contains("/**/", StringComparison.Ordinal)
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;
        string root = directoryPart.Replace("/**", string.Empty).TrimEnd('/');
        string searchRoot;
        try {
            searchRoot = string.IsNullOrWhiteSpace(root)
                ? projectDirectory
                : Path.GetFullPath(root.Replace('/', Path.DirectorySeparatorChar), projectDirectory);
        }
        catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException) {
            return [];
        }

        return Directory.Exists(searchRoot) ? Directory.EnumerateFiles(searchRoot, filePattern, search) : [];
    }

    private static bool IsExcluded(string relativePath, string[] excludes) {
        foreach (string exclude in excludes) {
            string normalized = exclude.Replace('\\', '/').Trim('/');
            if (normalized.EndsWith("/**", StringComparison.Ordinal)) {
                string prefix = normalized[..^3].TrimEnd('/') + "/";
                if (relativePath.StartsWith(prefix, PathUtilities.PathComparison)) {
                    return true;
                }
            }
            else if (string.Equals(relativePath, normalized, PathUtilities.PathComparison)) {
                return true;
            }
        }

        return false;
    }
}
