namespace Hexalith.FrontComposer.Cli;

internal static class PathUtilities
{
    private static readonly string[] ExcludedSegments = ["bin", "obj", ".git", ".hg", ".svn", "packages", ".nuget", "nupkgs"];
    public static StringComparison PathComparison => OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    public static StringComparer PathComparer => OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    public static string ToProjectRelative(string root, string path)
    {
        string fullRoot = EnsureTrailingSeparator(Path.GetFullPath(root));
        string fullPath = Path.GetFullPath(path);
        if (!fullPath.StartsWith(fullRoot, PathComparison)) {
            return "[redacted-path]";
        }

        return fullPath[fullRoot.Length..].Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
    }

    public static bool HasExcludedSegment(string root, string path)
    {
        string relative = ToProjectRelative(root, path);
        return relative.Split('/').Any(segment => ExcludedSegments.Contains(segment, StringComparer.OrdinalIgnoreCase))
            || relative.Contains("/generated/", StringComparison.OrdinalIgnoreCase);
    }

    public static string RedactAbsolute(string path) => Path.IsPathRooted(path) ? "[redacted-path]/" + Path.GetFileName(path) : path;

    public static string Canonical(string path)
    {
        string fullPath = Path.GetFullPath(path);
        try {
            if (File.Exists(fullPath)) {
                FileInfo file = new(fullPath);
                FileSystemInfo? target = file.ResolveLinkTarget(returnFinalTarget: true);
                return Path.GetFullPath(target?.FullName ?? file.FullName);
            }

            if (Directory.Exists(fullPath)) {
                DirectoryInfo directory = new(fullPath);
                FileSystemInfo? target = directory.ResolveLinkTarget(returnFinalTarget: true);
                return Path.GetFullPath(target?.FullName ?? directory.FullName);
            }
        }
        catch (IOException) {
        }
        catch (UnauthorizedAccessException) {
        }

        return fullPath;
    }

    private static string EnsureTrailingSeparator(string path)
        => path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
}
