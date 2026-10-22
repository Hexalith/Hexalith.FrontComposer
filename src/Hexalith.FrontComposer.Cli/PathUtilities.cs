namespace Hexalith.FrontComposer.Cli;

internal static class PathUtilities {
    public const string RedactedPathSentinel = "[redacted-path]";

    private static readonly string[] ExcludedSegments = ["bin", "obj", ".git", ".hg", ".svn", "packages", ".nuget", "nupkgs"];
    public static StringComparison PathComparison => OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    public static StringComparer PathComparer => OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    public static string ToProjectRelative(string root, string path) {
        string fullRoot = EnsureTrailingSeparator(Path.GetFullPath(root));
        string fullPath = Path.GetFullPath(path);
        if (!fullPath.StartsWith(fullRoot, PathComparison)) {
            return RedactedPathSentinel;
        }

        return fullPath[fullRoot.Length..].Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
    }

    public static bool HasExcludedSegment(string root, string path) {
        string relative = ToProjectRelative(root, path);
        return relative.Split('/').Any(segment => ExcludedSegments.Contains(segment, StringComparer.OrdinalIgnoreCase))
            || relative.Contains("/generated/", StringComparison.OrdinalIgnoreCase);
    }

    public static string RedactAbsolute(string path) => Path.IsPathRooted(path) ? RedactedPathSentinel + "/" + Path.GetFileName(path) : path;

    /// <summary>
    /// Refuses file links and linked ancestors below the project directory, including directory junctions,
    /// without reading source bytes. Inaccessible path metadata is unsafe as well.
    /// </summary>
    public static bool HasLinkedPathSegment(string projectDirectory, string path)
    {
        try
        {
            string projectRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(projectDirectory));
            string? current = Path.GetFullPath(path);
            while (current is not null && !string.Equals(current, projectRoot, PathComparison))
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                {
                    return true;
                }

                current = Path.GetDirectoryName(current);
            }

            return false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return true;
        }
    }

    public static string Canonical(string path) {
        string fullPath;
        try {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException) {
            return path;
        }

        try {
            // Resolve ancestors first so a stable lexical path cannot hide a changed workspace
            // link or project directory between the planner snapshot and apply revalidation.
            string normalizedPath = Path.TrimEndingDirectorySeparator(fullPath);
            string? parent = Path.GetDirectoryName(normalizedPath);
            if (parent is null) {
                return fullPath;
            }

            string resolvedPath = Path.Combine(Canonical(parent), Path.GetFileName(normalizedPath));
            FileSystemInfo? entry = Directory.Exists(resolvedPath)
                ? new DirectoryInfo(resolvedPath)
                : File.Exists(resolvedPath) ? new FileInfo(resolvedPath) : null;
            FileSystemInfo? target = entry?.ResolveLinkTarget(returnFinalTarget: true);
            return target is null ? resolvedPath : Canonical(target.FullName);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or PathTooLongException or NotSupportedException) {
        }

        return fullPath;
    }

    private static string EnsureTrailingSeparator(string path)
        => path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
}
