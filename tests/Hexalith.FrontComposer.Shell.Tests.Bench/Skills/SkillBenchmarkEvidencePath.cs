namespace Hexalith.FrontComposer.Mcp.Skills;

public static class SkillBenchmarkEvidencePath {
    public static string NormalizeUnderRoot(string evidenceRoot, string artifactName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactName);

        if (Path.IsPathRooted(artifactName)) {
            throw new InvalidOperationException("evidence path must be relative");
        }

        string root = Path.GetFullPath(evidenceRoot);
        string candidate = Path.GetFullPath(Path.Combine(root, artifactName));
        string comparisonRoot = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(comparisonRoot, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) {
            throw new InvalidOperationException("evidence path escapes approved root");
        }

        return candidate;
    }
}
