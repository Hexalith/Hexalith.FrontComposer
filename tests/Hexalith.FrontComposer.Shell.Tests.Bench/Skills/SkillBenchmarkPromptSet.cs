using System.Reflection;
using System.Text.Json;

namespace Hexalith.FrontComposer.Mcp.Skills;

public sealed record SkillBenchmarkPromptSet(
    string Version,
    IReadOnlyList<SkillBenchmarkPrompt> Prompts) {
    public static SkillBenchmarkPromptSet LoadEmbeddedV1() {
        Assembly assembly = typeof(SkillBenchmarkPromptSet).Assembly;
        // P-21: use stricter prefix and FirstOrDefault with deterministic ordering. Two
        // assemblies that incorrectly embed the same prompt-set name would otherwise crash
        // SingleOrDefault with an opaque InvalidOperationException at first benchmark call.
        string[] candidates = [.. assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith("Hexalith.FrontComposer.Mcp.Skills.benchmark-prompts.v1.", StringComparison.Ordinal)
                && n.EndsWith("prompt-set.json", StringComparison.Ordinal))
            .OrderBy(n => n, StringComparer.Ordinal)];

        if (candidates.Length == 0) {
            // Silent empty load is a footgun: a build that strips embedded resources would
            // ship a benchmark with zero prompts and report "100% pass" for an empty set.
            throw new InvalidOperationException(
                "Embedded benchmark prompt set 'Hexalith.FrontComposer.Mcp.Skills.benchmark-prompts.v1.prompt-set.json' is missing.");
        }

        string resourceName = candidates[0];
        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("Embedded benchmark prompt set is missing.");
        SkillBenchmarkPromptSetDto dto = JsonSerializer.Deserialize(stream, SkillBenchmarkJsonContext.Default.SkillBenchmarkPromptSetDto)
            ?? throw new InvalidOperationException("Embedded benchmark prompt set is invalid.");

        return new SkillBenchmarkPromptSet(
            dto.Version,
            [.. dto.Prompts
                .OrderBy(p => p.Id, StringComparer.Ordinal)
                .Select(p => new SkillBenchmarkPrompt(p.Id, p.Text, p.ExpectedShape))]);
    }
}
