using System.Text;

namespace Hexalith.FrontComposer.Cli;

internal static class SubmoduleBoundaryReader {
    // 64 ancestors covers every realistic checkout depth (Windows MAX_PATH is 260, average path
    // segment length is well above 4 chars, so a 64-deep tree already exceeds Windows path limits).
    // The cap prevents pathological filesystems from spinning forever; the limit is documented so
    // future readers know it is intentional rather than arbitrary.
    private const int MaxAncestorWalk = 64;

    public static HashSet<string> Read(string projectDirectory) {
        HashSet<string> roots = new(PathUtilities.PathComparer);
        string? repositoryRoot = FindRepositoryRoot(projectDirectory);
        if (repositoryRoot is null) {
            return roots;
        }

        string gitmodules = Path.Combine(repositoryRoot, ".gitmodules");
        if (!File.Exists(gitmodules)) {
            return roots;
        }

        IEnumerable<string> lines;
        try {
            // Materialize so that an IOException during enumeration cannot abort planning later.
            lines = File.ReadAllLines(gitmodules);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            // Treat an unreadable .gitmodules as "no submodule boundaries detected"; planning continues
            // and the WriteSafetyPolicy still excludes generated/bin/obj/etc. via PathUtilities.
            return roots;
        }

        bool inSubmoduleSection = false;
        foreach (string line in lines) {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("[", StringComparison.Ordinal)) {
                inSubmoduleSection = trimmed.StartsWith("[submodule ", StringComparison.Ordinal);
                continue;
            }

            if (!inSubmoduleSection || !trimmed.StartsWith("path", StringComparison.Ordinal)) {
                continue;
            }

            int equals = trimmed.IndexOf('=', StringComparison.Ordinal);
            if (equals < 0) {
                continue;
            }

            string relative = UnquoteGitConfigValue(trimmed[(equals + 1)..].Trim());
            // Reject `..` traversal and absolute paths in `.gitmodules` entries; a malicious or
            // hand-edited file should not be able to mark arbitrary ancestors as submodules.
            if (relative.Length == 0 || Path.IsPathRooted(relative) || relative.Replace('\\', '/').Split('/').Any(s => s == "..")) {
                continue;
            }

            _ = roots.Add(PathUtilities.Canonical(Path.Combine(repositoryRoot, relative.Replace('/', Path.DirectorySeparatorChar))));
        }

        return roots;
    }

    private static string UnquoteGitConfigValue(string value) {
        if (value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\''))) {
            value = value[1..^1];
        }

        StringBuilder builder = new(value.Length);
        bool escaping = false;
        foreach (char ch in value) {
            if (escaping) {
                _ = builder.Append(ch switch {
                    '"' => '"',
                    '\'' => '\'',
                    '\\' => '\\',
                    'n' => '\n',
                    't' => '\t',
                    _ => ch,
                });
                escaping = false;
                continue;
            }

            if (ch == '\\') {
                escaping = true;
                continue;
            }

            _ = builder.Append(ch);
        }

        if (escaping) {
            _ = builder.Append('\\');
        }

        return builder.ToString();
    }

    private static string? FindRepositoryRoot(string start) {
        DirectoryInfo? directory = new(Path.GetFullPath(start));
        int walked = 0;
        while (directory is not null && walked < MaxAncestorWalk) {
            string gitMarker = Path.Combine(directory.FullName, ".git");
            if (Directory.Exists(gitMarker) || File.Exists(gitMarker) || File.Exists(Path.Combine(directory.FullName, ".gitmodules"))) {
                return directory.FullName;
            }

            directory = directory.Parent;
            walked++;
        }

        return null;
    }
}
