using System.Text.Json;
using System.Text.RegularExpressions;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Docs;

/// <summary>
/// Story 9-5 documentation governance tests. The PowerShell validator is the runtime gate;
/// these tests keep the checked-in docs contract visible in the normal .NET test suite.
/// </summary>
[Trait("Category", "Governance")]
public sealed partial class DocsSiteValidationTests {
    private static readonly string[] RequiredFields =
        ["title", "description", "genre", "audience", "ownerStory", "status", "reviewed"];

    [Fact]
    public void ToolManifestPinsDocfxVersion() {
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(ProjectRoot(), ".config", "dotnet-tools.json")));
        string version = manifest.RootElement
            .GetProperty("tools")
            .GetProperty("docfx")
            .GetProperty("version")
            .GetString()!;

        version.ShouldBe("2.78.5");
    }

    [Fact]
    public void TocKeepsDiataxisTopLevelNavigation() {
        string toc = File.ReadAllText(Path.Combine(ProjectRoot(), "docs", "toc.yml"));

        toc.ShouldContain("name: Tutorials");
        toc.ShouldContain("name: How-to");
        toc.ShouldContain("name: Reference");
        toc.ShouldContain("name: Concepts");
    }

    [Fact]
    public void ContentPagesDeclareRequiredFrontMatterAndStableIdentities() {
        Dictionary<string, string> uids = new(StringComparer.Ordinal);
        Dictionary<string, string> slugs = new(StringComparer.Ordinal);

        foreach (string file in ContentFiles()) {
            Dictionary<string, string> frontMatter = ReadFrontMatter(file);
            string relative = Relative(file);

            foreach (string field in RequiredFields) {
                frontMatter.ContainsKey(field).ShouldBeTrue($"{relative} must declare {field}.");
                frontMatter[field].ShouldNotBeNullOrWhiteSpace($"{relative} must not leave {field} empty.");
            }

            (frontMatter.ContainsKey("uid") || frontMatter.ContainsKey("slug")).ShouldBeTrue($"{relative} needs uid or slug.");

            if (frontMatter.TryGetValue("uid", out string? uid)) {
                string canonical = Canonical(uid);
                uids.TryAdd(canonical, relative).ShouldBeTrue($"{relative} uid collides with {uids.GetValueOrDefault(canonical)}.");
            }

            if (frontMatter.TryGetValue("slug", out string? slug)) {
                string canonical = Canonical(slug);
                slugs.TryAdd(canonical, relative).ShouldBeTrue($"{relative} slug collides with {slugs.GetValueOrDefault(canonical)}.");
            }
        }
    }

    [Fact]
    public void McpReferenceExtractionKeepsReferenceAndStripsNarrative() {
        string page = File.ReadAllText(Path.Combine(ProjectRoot(), "docs", "reference", "mcp", "index.md"));

        string reference = HfcReferenceRegion().Match(page).Groups[1].Value;

        reference.ShouldContain("MCP slices retain bounded tables");
        reference.ShouldNotContain("Human docs keep onboarding");
    }

    [Fact]
    public void DiagnosticRegistryEntriesHavePublishedPagesWithRequiredSections() {
        using JsonDocument registry = JsonDocument.Parse(File.ReadAllText(Path.Combine(ProjectRoot(), "docs", "diagnostics", "diagnostic-registry.json")));

        foreach (JsonElement diagnostic in registry.RootElement.GetProperty("diagnostics").EnumerateArray()) {
            string lifecycle = diagnostic.GetProperty("lifecycle").GetString()!;
            if (lifecycle is not ("active" or "reserved" or "deprecated")) {
                continue;
            }

            string id = diagnostic.GetProperty("id").GetString()!;
            string docsSlug = diagnostic.GetProperty("docsSlug").GetString()!;
            string pagePath = Path.Combine(ProjectRoot(), "docs", docsSlug.Replace('/', Path.DirectorySeparatorChar) + ".md");
            File.Exists(pagePath).ShouldBeTrue($"{id} must publish {docsSlug}.md.");

            string page = File.ReadAllText(pagePath);
            foreach (string section in new[] { "## Problem", "## Common Causes", "## How To Fix", "## Example", "## Suppression Guidance", "## Migration/Deprecation", "## Related Diagnostics" }) {
                page.ShouldContain(section, customMessage: $"{id} must include {section}.");
            }
        }
    }

    [GeneratedRegex("(?s)<!--\\s*hfc:reference:start\\s*-->(.*?)<!--\\s*hfc:reference:end\\s*-->")]
    private static partial Regex HfcReferenceRegion();

    private static IEnumerable<string> ContentFiles() {
        string docs = Path.Combine(ProjectRoot(), "docs");
        string[] roots = ["tutorials", "how-to", "reference", "concepts", "migrations"];

        yield return Path.Combine(docs, "index.md");
        foreach (string root in roots) {
            foreach (string file in Directory.EnumerateFiles(Path.Combine(docs, root), "*.md", SearchOption.AllDirectories)) {
                yield return file;
            }
        }

        foreach (string file in Directory.EnumerateFiles(Path.Combine(docs, "diagnostics"), "HFC*.md", SearchOption.TopDirectoryOnly)) {
            yield return file;
        }
    }

    private static Dictionary<string, string> ReadFrontMatter(string file) {
        string text = File.ReadAllText(file);
        Match match = Regex.Match(text, "\\A---\\r?\\n(.*?)\\r?\\n---", RegexOptions.Singleline);
        match.Success.ShouldBeTrue($"{Relative(file)} must start with YAML front matter.");

        Dictionary<string, string> values = new(StringComparer.Ordinal);
        foreach (string line in match.Groups[1].Value.Split('\n')) {
            Match pair = Regex.Match(line.TrimEnd('\r'), "^([A-Za-z][A-Za-z0-9_-]*)\\s*:\\s*(.*?)\\s*$");
            if (!pair.Success) {
                continue;
            }

            values[pair.Groups[1].Value] = pair.Groups[2].Value.Trim().Trim('"');
        }

        return values;
    }

    private static string Canonical(string value)
        => Regex.Replace(Uri.UnescapeDataString(value).Normalize().Trim().TrimEnd('/').ToLowerInvariant(), @"[\\/_\-\.\s]+", "");

    private static string Relative(string path)
        => Path.GetRelativePath(ProjectRoot(), path).Replace('\\', '/');

    private static string ProjectRoot() {
        DirectoryInfo directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.FrontComposer.sln"))) {
            directory = directory.Parent!;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate project root.");
    }
}
