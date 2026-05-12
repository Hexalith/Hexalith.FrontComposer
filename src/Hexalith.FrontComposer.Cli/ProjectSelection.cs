namespace Hexalith.FrontComposer.Cli;

internal sealed record ProjectSelection(bool Success, string? ProjectPath, string Error)
{
    public static ProjectSelection Resolve(CommandOptions options, string currentDirectory)
    {
        string? explicitProject = options.Get("project");
        if (!string.IsNullOrWhiteSpace(explicitProject)) {
            string fullPath = PathUtilities.Canonical(Path.GetFullPath(explicitProject, currentDirectory));
            if (Directory.Exists(fullPath)) {
                return new ProjectSelection(false, null, "--project must resolve to a .csproj file, not a directory: " + OutputSanitizer.Sanitize(PathUtilities.RedactAbsolute(fullPath)));
            }

            string extension = Path.GetExtension(fullPath);
            if (!extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase)) {
                return extension.Equals(".fsproj", StringComparison.OrdinalIgnoreCase)
                    ? new ProjectSelection(false, null, ".fsproj is not supported by frontcomposer migrate/inspect v1; pass a .csproj project.")
                    : new ProjectSelection(false, null, "--project must resolve to a .csproj file: " + OutputSanitizer.Sanitize(PathUtilities.RedactAbsolute(fullPath)));
            }

            return File.Exists(fullPath)
                ? new ProjectSelection(true, fullPath, string.Empty)
                : new ProjectSelection(false, null, "Project file was not found: " + OutputSanitizer.Sanitize(PathUtilities.RedactAbsolute(fullPath)));
        }

        string? solution = options.Get("solution");
        if (!string.IsNullOrWhiteSpace(solution)) {
            string solutionPath = PathUtilities.Canonical(Path.GetFullPath(solution, currentDirectory));
            string extension = Path.GetExtension(solutionPath);
            if (!extension.Equals(".sln", StringComparison.OrdinalIgnoreCase)) {
                return extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase)
                    ? new ProjectSelection(false, null, ".slnx is not supported by frontcomposer migrate/inspect v1; pass --project with a .csproj file.")
                    : new ProjectSelection(false, null, "--solution must resolve to a .sln file: " + OutputSanitizer.Sanitize(PathUtilities.RedactAbsolute(solutionPath)));
            }

            if (!File.Exists(solutionPath)) {
                return new ProjectSelection(false, null, "Solution file was not found: " + OutputSanitizer.Sanitize(PathUtilities.RedactAbsolute(solutionPath)));
            }

            string solutionDirectory = Path.GetDirectoryName(solutionPath)!;
            SolutionProjectParseResult parse = ReadSolutionProjects(solutionPath);
            if (parse.MalformedLines > 0) {
                return new ProjectSelection(false, null, "Solution contains malformed Project entries; pass --project with a .csproj file.");
            }

            if (parse.UnsupportedProjectTypes.Length > 0) {
                return new ProjectSelection(false, null, string.Join(", ", parse.UnsupportedProjectTypes.Order(StringComparer.OrdinalIgnoreCase))
                    + " is not supported by frontcomposer migrate/inspect v1. Solution contains unsupported project type(s) "
                    + string.Join(", ", parse.UnsupportedProjectTypes.Order(StringComparer.OrdinalIgnoreCase))
                    + "; pass --project with a .csproj file.");
            }

            string[] canonicalProjects = parse.ProjectPaths
                .Select(path => PathUtilities.Canonical(Path.GetFullPath(path, solutionDirectory)))
                .ToArray();
            if (canonicalProjects.Any(path => !IsSameOrUnder(solutionDirectory, path))) {
                return new ProjectSelection(false, null, "Solution contains project paths outside the solution directory; pass --project with a repository-local .csproj file.");
            }

            string[] projects = canonicalProjects
                .Where(File.Exists)
                .Order(StringComparer.Ordinal)
                .ToArray();
            return projects.Length switch {
                1 => new ProjectSelection(true, projects[0], string.Empty),
                0 => new ProjectSelection(false, null, "No C# projects were found in the solution."),
                _ => new ProjectSelection(false, null, "Solution contains multiple projects; pass --project. Choices: " + string.Join(", ", projects.Select(path => OutputSanitizer.Sanitize(PathUtilities.ToProjectRelative(solutionDirectory, path))))),
            };
        }

        string[] localProjects = Directory.EnumerateFiles(currentDirectory, "*.csproj", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.Ordinal)
            .ToArray();
        return localProjects.Length switch {
            1 => new ProjectSelection(true, localProjects[0], string.Empty),
            0 => new ProjectSelection(false, null, "No project was specified and no project file was found in the current directory."),
            _ => new ProjectSelection(false, null, "Current directory contains multiple projects; pass --project."),
        };
    }

    private static SolutionProjectParseResult ReadSolutionProjects(string solutionPath)
    {
        List<string> projectPaths = [];
        HashSet<string> unsupported = new(StringComparer.OrdinalIgnoreCase);
        int malformed = 0;

        foreach (string line in File.ReadLines(solutionPath)) {
            if (!line.StartsWith("Project(", StringComparison.Ordinal)) {
                continue;
            }

            string[] fields = ReadQuotedFields(line);
            if (fields.Length < 3) {
                malformed++;
                continue;
            }

            string projectPath = fields[2];
            string extension = Path.GetExtension(projectPath);
            if (extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase)) {
                projectPaths.Add(projectPath);
            }
            else if (!string.IsNullOrWhiteSpace(extension)) {
                _ = unsupported.Add(extension);
            }
        }

        return new SolutionProjectParseResult(projectPaths.ToArray(), unsupported.ToArray(), malformed);
    }

    private static string[] ReadQuotedFields(string line)
    {
        List<string> fields = [];
        for (int i = 0; i < line.Length; i++) {
            if (line[i] != '"') {
                continue;
            }

            i++;
            System.Text.StringBuilder builder = new();
            while (i < line.Length) {
                if (line[i] == '"') {
                    if (i + 1 < line.Length && line[i + 1] == '"') {
                        _ = builder.Append('"');
                        i += 2;
                        continue;
                    }

                    break;
                }

                _ = builder.Append(line[i]);
                i++;
            }

            if (i >= line.Length || line[i] != '"') {
                return [];
            }

            fields.Add(builder.ToString());
        }

        return fields.ToArray();
    }

    private sealed record SolutionProjectParseResult(
        string[] ProjectPaths,
        string[] UnsupportedProjectTypes,
        int MalformedLines);

    private static bool IsSameOrUnder(string root, string path)
    {
        string normalizedRoot = PathUtilities.Canonical(root);
        string normalizedPath = PathUtilities.Canonical(path);
        if (string.Equals(normalizedPath, normalizedRoot, PathUtilities.PathComparison)) {
            return true;
        }

        string rootWithSeparator = normalizedRoot.EndsWith(Path.DirectorySeparatorChar)
            ? normalizedRoot
            : normalizedRoot + Path.DirectorySeparatorChar;
        return normalizedPath.StartsWith(rootWithSeparator, PathUtilities.PathComparison);
    }
}
