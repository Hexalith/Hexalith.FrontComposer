namespace Hexalith.FrontComposer.Cli;

internal static class PathUtilities
{
    private static readonly string[] ExcludedSegments = ["bin", "obj", ".git", ".hg", ".svn", "packages", ".nuget", "nupkgs"];

    public static string ToProjectRelative(string root, string path)
    {
        string fullRoot = EnsureTrailingSeparator(Path.GetFullPath(root));
        string fullPath = Path.GetFullPath(path);
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase)) {
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

    public static string Canonical(string path) => Path.GetFullPath(path);

    private static string EnsureTrailingSeparator(string path)
        => path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
}
