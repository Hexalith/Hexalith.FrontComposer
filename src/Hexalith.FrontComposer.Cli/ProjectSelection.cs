namespace Hexalith.FrontComposer.Cli;

internal sealed record ProjectSelection(bool Success, string? ProjectPath, string Error)
{
    public static ProjectSelection Resolve(CommandOptions options, string currentDirectory)
    {
        string? explicitProject = options.Get("project");
        if (!string.IsNullOrWhiteSpace(explicitProject)) {
            string fullPath = Path.GetFullPath(explicitProject, currentDirectory);
            if (Directory.Exists(fullPath)) {
                return new ProjectSelection(false, null, "--project must resolve to a .csproj file, not a directory: " + OutputSanitizer.Sanitize(PathUtilities.RedactAbsolute(fullPath)));
            }

            return File.Exists(fullPath)
                ? Path.GetExtension(fullPath).Equals(".csproj", StringComparison.OrdinalIgnoreCase)
                    ? new ProjectSelection(true, fullPath, string.Empty)
                    : new ProjectSelection(false, null, "--project must resolve to a .csproj file: " + OutputSanitizer.Sanitize(PathUtilities.RedactAbsolute(fullPath)))
                : new ProjectSelection(false, null, "Project file was not found: " + OutputSanitizer.Sanitize(PathUtilities.RedactAbsolute(fullPath)));
        }

        string? solution = options.Get("solution");
        if (!string.IsNullOrWhiteSpace(solution)) {
            string solutionPath = Path.GetFullPath(solution, currentDirectory);
            if (!File.Exists(solutionPath)) {
                return new ProjectSelection(false, null, "Solution file was not found: " + OutputSanitizer.Sanitize(PathUtilities.RedactAbsolute(solutionPath)));
            }

            string solutionDirectory = Path.GetDirectoryName(solutionPath)!;
            string[] projects = File.ReadLines(solutionPath)
                .Select(TryReadProjectPath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => Path.GetFullPath(path!, solutionDirectory))
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

    private static string? TryReadProjectPath(string line)
    {
        if (!line.StartsWith("Project(", StringComparison.Ordinal) || !line.Contains(".csproj", StringComparison.OrdinalIgnoreCase)) {
            return null;
        }

        string[] parts = line.Split('"');
        return parts.FirstOrDefault(part => part.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));
    }
}
