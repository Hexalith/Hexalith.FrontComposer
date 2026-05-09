namespace Hexalith.FrontComposer.Cli;

internal static class PathUtilities
{
    public const string RedactedPathSentinel = "[redacted-path]";

    private static readonly string[] ExcludedSegments = ["bin", "obj", ".git", ".hg", ".svn", "packages", ".nuget", "nupkgs"];
    public static StringComparison PathComparison => OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    public static StringComparer PathComparer => OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    public static string ToProjectRelative(string root, string path)
    {
        string fullRoot = EnsureTrailingSeparator(Path.GetFullPath(root));
        string fullPath = Path.GetFullPath(path);
        if (!fullPath.StartsWith(fullRoot, PathComparison)) {
            return RedactedPathSentinel;
        }

        return fullPath[fullRoot.Length..].Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
    }

    public static bool HasExcludedSegment(string root, string path)
    {
        string relative = ToProjectRelative(root, path);
        return relative.Split('/').Any(segment => ExcludedSegments.Contains(segment, StringComparer.OrdinalIgnoreCase))
            || relative.Contains("/generated/", StringComparison.OrdinalIgnoreCase);
    }

    public static string RedactAbsolute(string path) => Path.IsPathRooted(path) ? RedactedPathSentinel + "/" + Path.GetFileName(path) : path;

    public static string Canonical(string path)
    {
        string fullPath;
        try {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException) {
            return path;
        }

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

            string? parent = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent)) {
                DirectoryInfo parentInfo = new(parent);
                FileSystemInfo? parentTarget = parentInfo.ResolveLinkTarget(returnFinalTarget: true);
                string parentResolved = Path.GetFullPath(parentTarget?.FullName ?? parentInfo.FullName);
                return Path.GetFullPath(Path.Combine(parentResolved, Path.GetFileName(fullPath)));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or PathTooLongException or NotSupportedException) {
        }

        return fullPath;
    }

    private static string EnsureTrailingSeparator(string path)
        => path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
}
