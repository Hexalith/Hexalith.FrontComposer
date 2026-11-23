using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Hexalith.FrontComposer.Mcp.Skills;

public sealed record SkillBenchmarkPromptSet(
    string Version,
    IReadOnlyList<SkillBenchmarkPrompt> Prompts) {
    /// <summary>Computes the v1 corpus hash used by eng/llm_benchmark.py for the ordered prompts.</summary>
    public string ComputeCorpusHash() {
        // Python hashes compact JSON with sorted object keys and ASCII escapes. Array order is significant.
        var json = new StringBuilder("{\"prompts\":[");
        for (int i = 0; i < Prompts.Count; i++) {
            if (i > 0) {
                _ = json.Append(',');
            }

            SkillBenchmarkPrompt prompt = Prompts[i];
            _ = json.Append("{\"expectedShape\":[")
                .AppendJoin(',', prompt.ExpectedShape.Select(QuoteJsonString))
                .Append("],\"id\":").Append(QuoteJsonString(prompt.Id))
                .Append(",\"text\":").Append(QuoteJsonString(prompt.Text)).Append('}');
        }

        _ = json.Append("],\"version\":").Append(QuoteJsonString(Version)).Append('}');
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(json.ToString())));
    }

    private static string QuoteJsonString(string value) {
        var quoted = new StringBuilder("\"");
        foreach (char c in value) {
            _ = c switch {
                '"' => quoted.Append("\\\""),
                '\\' => quoted.Append("\\\\"),
                '\b' => quoted.Append("\\b"),
                '\t' => quoted.Append("\\t"),
                '\n' => quoted.Append("\\n"),
                '\f' => quoted.Append("\\f"),
                '\r' => quoted.Append("\\r"),
                < ' ' or > '~' => quoted.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture)),
                _ => quoted.Append(c),
            };
        }

        return quoted.Append('"').ToString();
    }

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
        return Load(stream);
    }

    internal static SkillBenchmarkPromptSet Load(Stream stream) {
        SkillBenchmarkPromptSetDto dto = JsonSerializer.Deserialize(stream, SkillBenchmarkJsonContext.Default.SkillBenchmarkPromptSetDto)
            ?? throw new InvalidOperationException("Embedded benchmark prompt set is invalid.");

        if (dto.Prompts.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count() != dto.Prompts.Count) {
            throw new InvalidOperationException("Benchmark prompt IDs must be unique.");
        }

        return new SkillBenchmarkPromptSet(
            dto.Version,
            [.. dto.Prompts
                .OrderBy(p => p.Id, StringComparer.Ordinal)
                .Select(p => new SkillBenchmarkPrompt(p.Id, p.Text, p.ExpectedShape))]);
    }
}
