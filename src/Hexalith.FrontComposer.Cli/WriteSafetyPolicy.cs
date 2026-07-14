namespace Hexalith.FrontComposer.Cli;

internal static class WriteSafetyPolicy {
    public static bool IsAllowed(string projectDirectory, string path, HashSet<string> submoduleRoots) {
        string relative = PathUtilities.ToProjectRelative(projectDirectory, path);
        if (relative == PathUtilities.RedactedPathSentinel || PathUtilities.HasExcludedSegment(projectDirectory, path)) {
            return false;
        }

        string fullPath = PathUtilities.Canonical(path);
        return !submoduleRoots.Any(root => IsSameOrUnder(fullPath, root));
    }

    private static bool IsSameOrUnder(string path, string root) {
        string normalizedRoot = PathUtilities.Canonical(root);
        string normalizedPath = PathUtilities.Canonical(path);
        if (string.Equals(normalizedPath, normalizedRoot, PathUtilities.PathComparison)) {
            return true;
        }

        string rootWithSep = EnsureTrailingSeparator(normalizedRoot);
        return normalizedPath.StartsWith(rootWithSep, PathUtilities.PathComparison);
    }

    private static string EnsureTrailingSeparator(string path)
        => path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
}
