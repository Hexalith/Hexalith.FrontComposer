using System.Reflection;
using System.Text;

namespace Hexalith.FrontComposer.Mcp.Skills;

public static class SkillCorpusLoader {
    private const string SkillResourcePrefix = "Hexalith.FrontComposer.Mcp.Skills.";

    public static SkillCorpusSnapshot LoadEmbedded() {
        Assembly assembly = typeof(SkillCorpusLoader).Assembly;
        SkillCorpusSource[] sources = [.. assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(SkillResourcePrefix, StringComparison.Ordinal) && name.EndsWith(".md", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .Select(name => new SkillCorpusSource(ToPath(name), ReadResource(assembly, name)))];

        return SkillCorpusParser.Parse(sources);
    }

    // P-1: extension-aware path reconstruction. Embedded resource names use `.` both as a
    // namespace separator AND as the literal file extension dot, so a naive `Replace('.', '/')`
    // mangles `*.md` to `*/md` and `v1.0.md` to `v1/0/md`. Strip the final extension first,
    // replace the remaining dots and platform path separators with `/`, then re-append the
    // extension. Cross-platform note: MSBuild's %(RecursiveDir) emits `\` on Windows and `/` on
    // Linux, so the embedded resource name can contain either separator before the file part;
    // both collapse to forward slash here for stable diagnostic paths.
    private static string ToPath(string resourceName) {
        string relative = resourceName[SkillResourcePrefix.Length..];
        int lastDot = relative.LastIndexOf('.');
        if (lastDot < 0) {
            return "docs/skills/frontcomposer/" + relative.Replace('\\', '/');
        }

        string nameWithoutExt = relative[..lastDot];
        string extension = relative[lastDot..];
        string normalized = nameWithoutExt.Replace('.', '/').Replace('\\', '/');
        return "docs/skills/frontcomposer/" + normalized + extension;
    }

    private static string ReadResource(Assembly assembly, string resourceName) {
        // The resource name was just enumerated from the same assembly; a missing stream
        // means the assembly is corrupt. Surfacing that as a hard failure beats silently
        // emitting an empty source that would be rejected with a misleading "missing front
        // matter" diagnostic.
        using Stream? stream = assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException($"Embedded skill resource '{resourceName}' is missing.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
