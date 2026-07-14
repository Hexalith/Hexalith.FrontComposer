using System.Reflection;

namespace Hexalith.FrontComposer.Mcp.Skills;

public static class SkillCorpusReferenceValidator {
    public static SkillCorpusValidationResult Validate(
        SkillCorpusSnapshot snapshot,
        IEnumerable<Assembly> publicApiAssemblies,
        string projectRoot = "") {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(publicApiAssemblies);

        List<SkillCorpusDiagnostic> diagnostics = [.. snapshot.Diagnostics];
        Assembly[] assemblies = [.. publicApiAssemblies];

        // P-23: when projectRoot is supplied, normalize it once so the under-root traversal
        // check below can compare full paths.
        string? rootFullPath = string.IsNullOrWhiteSpace(projectRoot) ? null : Path.GetFullPath(projectRoot);

        foreach (SkillCorpusResource resource in snapshot.Resources) {
            foreach (string reference in resource.PublicApiReferences) {
                if (FindType(reference, assemblies) is null) {
                    diagnostics.Add(new SkillCorpusDiagnostic(
                        SkillCorpusDiagnosticCategory.MissingPublicApiReference,
                        resource.SourceDoc,
                        $"Public API reference '{reference}' was not found."));
                }
            }

            if (rootFullPath is not null) {
                foreach (string samplePath in resource.SamplePaths) {
                    string candidate = samplePath.Replace('/', Path.DirectorySeparatorChar);
                    string fullPath = Path.GetFullPath(Path.Combine(rootFullPath, candidate));

                    // P-23: reject paths that escape projectRoot via `..` traversal.
                    if (!fullPath.StartsWith(rootFullPath + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                        && !string.Equals(fullPath, rootFullPath, StringComparison.Ordinal)) {
                        diagnostics.Add(new SkillCorpusDiagnostic(
                            SkillCorpusDiagnosticCategory.MissingSamplePath,
                            resource.SourceDoc,
                            $"Sample path '{samplePath}' resolves outside project root."));
                        continue;
                    }

                    if (!Directory.Exists(fullPath) && !File.Exists(fullPath)) {
                        diagnostics.Add(new SkillCorpusDiagnostic(
                            SkillCorpusDiagnosticCategory.MissingSamplePath,
                            resource.SourceDoc,
                            $"Sample path '{samplePath}' was not found."));
                    }
                }
            }
        }

        return new SkillCorpusValidationResult(diagnostics);
    }

    // P-22: drop the `Type.GetType` fallback that would resolve arbitrary loaded BCL types.
    // The validator's purpose is to keep skill examples scoped to the supplied framework
    // assemblies; admitting `System.*` types defeats it.
    private static Type? FindType(string fullName, IEnumerable<Assembly> assemblies) {
        foreach (Assembly assembly in assemblies) {
            Type? type = assembly.GetType(fullName, throwOnError: false, ignoreCase: false);
            if (type is not null) {
                return type;
            }
        }

        return null;
    }
}
