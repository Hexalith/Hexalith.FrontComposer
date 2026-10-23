using System.Diagnostics;
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
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(ProjectRoot(), ".config", "dotnet-tools.json")));
        string version = manifest.RootElement
            .GetProperty("tools")
            .GetProperty("docfx")
            .GetProperty("version")
            .GetString()!;

        version.ShouldBe("2.78.5");
    }

    [Fact]
    public void TocKeepsDiataxisTopLevelNavigation() {
        string[] tocLines = File.ReadAllLines(Path.Combine(ProjectRoot(), "docs", "toc.yml"));
        string[] topLevelNames = tocLines
            .Select(line => Regex.Match(line, "^- name:\\s*(.+?)\\s*$"))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value)
            .ToArray();

        topLevelNames.ShouldBe(["Tutorials", "How-to", "Reference", "Concepts"]);
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
        using var registry = JsonDocument.Parse(File.ReadAllText(Path.Combine(ProjectRoot(), "docs", "diagnostics", "diagnostic-registry.json")));

        foreach (JsonElement diagnostic in registry.RootElement.GetProperty("diagnostics").EnumerateArray()) {
            string lifecycle = diagnostic.GetProperty("lifecycle").GetString()!;
            if (lifecycle is not ("active" or "reserved" or "deprecated")) {
                continue;
            }

            string id = diagnostic.GetProperty("id").GetString()!;
            string docsSlug = diagnostic.GetProperty("docsSlug").GetString()!;
            docsSlug.ShouldBe($"diagnostics/{id}");
            docsSlug.ShouldMatch("^diagnostics/HFC[0-9A-Za-z_-]+$");
            docsSlug.ShouldNotContain("..");

            string pagePath = Path.GetFullPath(Path.Combine(ProjectRoot(), "docs", docsSlug.Replace('/', Path.DirectorySeparatorChar) + ".md"));
            string diagnosticsRoot = Path.GetFullPath(Path.Combine(ProjectRoot(), "docs", "diagnostics")) + Path.DirectorySeparatorChar;
            pagePath.StartsWith(diagnosticsRoot, StringComparison.OrdinalIgnoreCase).ShouldBeTrue($"{id} docsSlug must stay under docs/diagnostics.");
            File.Exists(pagePath).ShouldBeTrue($"{id} must publish {docsSlug}.md.");

            string page = File.ReadAllText(pagePath);
            foreach (string section in new[] { "## Problem", "## Common Causes", "## How To Fix", "## Example", "## Suppression Guidance", "## Migration/Deprecation", "## Related Diagnostics" }) {
                page.ShouldContain(section, customMessage: $"{id} must include {section}.");
            }

            page.ShouldNotContain("The framework detected a condition represented by", customMessage: $"{id} must not keep generated placeholder prose.");
            page.ShouldNotContain("Expected: Follow the FrontComposer diagnostic contract.", customMessage: $"{id} must not keep generated placeholder prose.");
        }
    }

    [Fact]
    public void ProducerFingerprintBaselineCoversRequiredInputs() {
        string path = Path.Combine(ProjectRoot(), "docs", "validation", "producer-fingerprints.json");
        File.Exists(path).ShouldBeTrue("Docs validation must compare producer inputs against a checked-in fingerprint baseline.");

        using var baseline = JsonDocument.Parse(File.ReadAllText(path));
        var paths = baseline.RootElement
            .GetProperty("producers")
            .EnumerateArray()
            .Select(item => item.GetProperty("path").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        foreach (string required in new[] {
            "docs/skills/frontcomposer/index.md",
            "docs/diagnostics/samples/registry-drift-report.json",
            "docs/migrations/9.1-to-9.2.md",
            "docs/ide-parity-matrix.md",
            "docs/diagnostics/diagnostic-registry.json",
        }) {
            paths.Contains(required).ShouldBeTrue($"{required} must have a producer fingerprint baseline.");
        }
    }

    [Fact]
    public void MigrationIndexPinsExactGuideVersionsAndClassifications()
    {
        string migrations = Path.Combine(ProjectRoot(), "docs", "migrations");
        string index = File.ReadAllText(Path.Combine(migrations, "index.md"));
        string toc = File.ReadAllText(Path.Combine(ProjectRoot(), "docs", "toc.yml"));
        foreach (string guide in Directory.EnumerateFiles(migrations, "*.md").Where(path => Path.GetFileName(path) != "index.md"))
        {
            Dictionary<string, string> front = ReadFrontMatter(guide);
            front["migrationMode"].ShouldBeOneOf("cli-supported", "manual-only");
            string row = $"| {front["fromVersion"]} | {front["toVersion"]} | {front["migrationMode"]} |";
            string[] linkedRows = index.Split('\n').Where(line => line.Contains($"]({Path.GetFileName(guide)})", StringComparison.Ordinal)).ToArray();
            linkedRows.Length.ShouldBe(1, $"{Relative(guide)} needs one classified index link.");
            linkedRows[0].ShouldStartWith(row);
            toc.ShouldContain($"href: migrations/{Path.GetFileName(guide)}");
        }

        ReadFrontMatter(Path.Combine(migrations, "3.1-to-4.0.md"))["fromVersion"].ShouldBe("3.1.1");
    }

    [Fact]
    public void MigrationGuidanceDescribesCliSafeFixAndSyntheticManualBoundary()
    {
        string guide = NormalizeWhitespace(File.ReadAllText(Path.Combine(ProjectRoot(), "docs", "migrations", "9.1-to-9.2.md")));
        ReadFrontMatter(Path.Combine(ProjectRoot(), "docs", "migrations", "9.1-to-9.2.md"))["codeFixAvailable"].ShouldBe("false");
        guide.ShouldContain("CLI-only safe fix");
        guide.ShouldContain("No IDE analyzer/code fix");
        guide.ShouldContain("`HFCM9002` is synthetic/manual-only");
        guide.ShouldContain("Production emission is not approved");
        guide.ShouldContain("--from 9.1.0 --to 9.2.0 --dry-run");

        string howTo = NormalizeWhitespace(File.ReadAllText(Path.Combine(ProjectRoot(), "docs", "how-to", "migration-guides.md")));
        howTo.ShouldContain("Dry-run is the default and writes nothing");
        howTo.ShouldContain("earlier successful writes remain");
        howTo.ShouldContain("applied=false");
        howTo.ShouldContain("file symlinks");
        howTo.ShouldContain("Compile` Link metadata does not authorize");
        foreach (string page in new[] { guide, howTo })
        {
            page.ShouldContain("IdentifierNameSyntax");
            page.ShouldContain("semantic validation");
            page.ShouldContain("nameof");
            page.ShouldContain("same-named non-API identifiers");
            page.ShouldContain("Review the diff");
            page.ShouldContain("../reference/components/customization.md");
            page.ShouldContain("compiled with `DEBUG`");
            page.ShouldContain("`IHostEnvironment` explicitly identifying");
            page.ShouldContain("Register logging and the environment before `AddFrontComposerDevMode()`");
            page.ShouldContain("published Release");
            page.ShouldContain("NuGet package does not emit these customization diagnostics/panels");
        }
        howTo.ShouldContain("`applied=false` on emitted reports");
        howTo.ShouldContain("Migration sidecars support arrays of diagnostic objects");
        howTo.ShouldContain("below the selected project directory are ineligible");
        howTo.ShouldContain("linked ancestor directories below the project directory");
        guide.ShouldContain("may make `migrate` throw outside handled failures");
        howTo.ShouldContain("before any report is emitted");
        howTo.ShouldContain("absent or partial report");
        howTo.ShouldContain("unsupported valid JSON shapes");
        howTo.ShouldContain("may throw outside handled failures");
    }

    [Fact]
    public void CliReferencePinsInspectCountsOrderingFilteringAndPathOptIn()
    {
        string reference = NormalizeWhitespace(File.ReadAllText(Path.Combine(ProjectRoot(), "docs", "reference", "cli.md")));
        reference.ShouldContain("frontcomposer.cli.inspect.v1");
        reference.ShouldContain("frontcomposer.cli.migrate.v1");
        reference.ShouldContain("`CommandForm`");
        reference.ShouldContain("`ProjectionRazor`");
        reference.ShouldContain("`Registration`");
        reference.ShouldContain("`TemplateManifest`");
        reference.ShouldContain("`mcpManifestEntries` counts files");
        reference.ShouldContain("share ordinal file ordering by related type, family name, then path");
        reference.ShouldContain("Both severity and type filters run before fail flags");
        reference.ShouldContain("warning sentinel `HFCM0002`");
        reference.ShouldContain("Unsupported valid JSON shapes");
        reference.ShouldContain("may throw outside handled failures");
        reference.ShouldContain("Handled CLI failures print messages without stack traces");
        reference.ShouldContain("echoed without escaping or a length budget");
        reference.ShouldContain("Single-line fields escape C0 controls (`U+0000`–`U+001F`) and DEL (`U+007F`)");
        reference.ShouldContain("retain carriage returns, line feeds, and tabs; they escape other C0 controls and DEL");
        reference.ShouldContain("C1 controls (`U+0080`–`U+009F`) remain unchanged");
        reference.ShouldNotContain("terminal-safe");
        reference.ShouldContain("filesystem exception messages may still contain private absolute paths");
        reference.ShouldContain("Migration sidecars also require the documented diagnostic-object shapes");
        reference.ShouldContain("Ordinary sidecar diagnostics without `relatedType` receive an empty string and are excluded by `--type`");
        reference.ShouldContain("linked ancestor directories/junctions below the selected project directory");
        reference.ShouldContain("Handled filesystem failures print exception messages with escaped control characters");
        reference.ShouldContain("those messages can include absolute filesystem paths");
        reference.ShouldContain("Migration sidecars support arrays of diagnostic objects");
        reference.ShouldContain("Global MCP/template manifests and the `HFCM0002` sentinel remain in type-filtered output");
        reference.ShouldContain("Ordinary sidecar diagnostics without a related type have an empty type string and are removed by `--type`");
        reference.ShouldContain("linked ancestor directories/junctions below the project directory");
        reference.ShouldNotContain("The only executable edge");
        reference.ShouldContain("`applied=false` on emitted reports");
        reference.ShouldContain("before any report is emitted");
        reference.ShouldContain("absent or partial report");
        reference.ShouldContain("`--absolute-paths`");
        reference.ShouldContain("project and sidecar paths retain their relative/redacted behavior");
        reference.ShouldContain("64,000-character aggregate budget");
        reference.ShouldContain("do not semantically scrub", Case.Insensitive);
        reference.ShouldNotContain("reports generated-output paths, project metadata, schema fingerprints");

        string readme = NormalizeWhitespace(File.ReadAllText(Path.Combine(ProjectRoot(), "src", "Hexalith.FrontComposer.Cli", "README.md")));
        readme.ShouldNotContain("terminal-safe");
        readme.ShouldNotContain("log-safe");
        readme.ShouldNotContain("bounded/redacted");
        readme.ShouldContain("not semantically scrubbed");
        readme.ShouldContain("may throw outside handled failures");
    }

    [Fact]
    public void Hfc1016DocumentationPinsUniformSetterAndFailClosedSuppressionContract() {
        string root = ProjectRoot();
        string page = File.ReadAllText(Path.Combine(root, "docs", "diagnostics", "HFC1016.md"));
        string registry = File.ReadAllText(Path.Combine(root, "docs", "diagnostics", "diagnostic-registry.json"));
        string apiContracts = File.ReadAllText(Path.Combine(root, "_bmad-output", "project-docs", "api-contracts.md"));
        string releases = File.ReadAllText(Path.Combine(root, "src", "Hexalith.FrontComposer.SourceTools", "AnalyzerReleases.Unshipped.md"));

        page.ShouldContain("Suppression cannot restore generation");
        page.ShouldContain("Breaking derivable-setter migration");
        page.ShouldContain("public non-init setter");
        registry.ShouldContain("suppression cannot restore command generation");
        registry.ShouldContain("including derivable properties");
        apiContracts.ShouldContain("including derivable properties");
        apiContracts.ShouldContain("Suppression cannot restore generation");
        apiContracts.ShouldContain("breaking derivable-setter migration");
        releases.ShouldContain("breaking derivable-setter migration");
        releases.ShouldContain("suppression cannot restore generation");
    }

    [Fact]
    public void FluentContingencyPreservesDatedDecisionAndReceiptLifecycle()
    {
        string page = NormalizeWhitespace(File.ReadAllText(Path.Combine(ProjectRoot(), "docs", "fluent-ui-v5-contingency.md")));
        page.ShouldContain("2026-10-21 Product Owner + Architect decision is not approved");
        page.ShouldContain("https://github.com/Hexalith/Hexalith.FrontComposer/blob/main/_bmad-output/implementation-artifacts/spec-13-9-decide-the-exact-fluent-ui-v5-posture.md#product-decision-receipt");
        page.ShouldContain("OI-4, FLUENT-APP-1, D-13 and G-4 remain open");
        page.ShouldContain("one green hosted Quality run");
        page.ShouldContain("main commit whose root gitlink selects the exact target identity");
        page.ShouldContain("Chromium, Firefox, WebKit, `accessibility-visual` and Gate 3a bUnit");
        page.ShouldContain("Product Owner + Architect** must make a dated re-decision citing that run");
        page.ShouldContain("Human assistive-technology, zoom and device observations remain separate evidence under **OI-16**");
        page.ShouldContain("Equal package versions across Builds revisions do not transfer evidence between candidates");
        page.ShouldContain("After acceptance");
        page.ShouldContain("Architect addendum");
        page.ShouldContain("minor or major");
        page.ShouldContain("new Product Owner + Architect decision");
        page.ShouldContain("breaking Fluent change requires a FrontComposer major version");
        page.ShouldContain("2026-09-30");
        page.ShouldContain("Fluent canary workflow is **not implemented**");
        page.ShouldContain("Hexalith.Builds repository");
        foreach (string name in new[] { "FluentProviders", "FluentAccordion", "FluentBadge", "FluentTooltip", "FluentStack", "FluentGrid", "FluentLayout", "IThemeService.SetThemeAsync(ThemeSettings)", "ThemeSettings(Color, HueTorsion, Vibrancy, Mode, IsExact)", "ThemeMode", "--colorBrandForeground1", "--colorStatusSuccessForeground1", "--colorStatusWarningForeground1", "--colorStatusDangerForeground1" })
        {
            page.ShouldContain(name);
        }
        page.ShouldContain("**Unsupported:** `--colorStatusInfoForeground1` does not resolve");
        page.ShouldContain("`--fc-color-info` alias is unresolvable");
        page.ShouldContain("DW-1986");
        page.ShouldContain("Legacy v4/FAST tokens");
        page.ShouldNotContain("5.0.0-rc.2");
        page.ShouldNotContain("DefaultValues");
        page.ShouldNotContain("FluentLayoutItem");
        page.ShouldNotContain("204 tests");
    }

    [Fact]
    public void FluentContingencyParticipatesInMetadataAndHowToPublication()
    {
        string page = Path.Combine(ProjectRoot(), "docs", "fluent-ui-v5-contingency.md");
        ContentFiles().ShouldContain(page);
        Dictionary<string, string> front = ReadFrontMatter(page);
        front["genre"].ShouldBe("how-to");
        front["ownerStory"].ShouldBe("15.4");
        front["status"].ShouldBe("authored");
        using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(ProjectRoot(), "docs", "docfx.json")));
        config.RootElement.GetProperty("build").GetProperty("content").EnumerateArray()
            .SelectMany(entry => entry.GetProperty("files").EnumerateArray())
            .Count(file => file.GetString() == "fluent-ui-v5-contingency.md").ShouldBe(1);
        string toc = File.ReadAllText(Path.Combine(ProjectRoot(), "docs", "toc.yml"));
        Regex.Match(toc, @"(?ms)^- name: How-to\r?\n(.*?)(?=^- name:|\z)").Groups[1].Value
            .ShouldContain("href: fluent-ui-v5-contingency.md");
    }

    [Theory]
    [InlineData("Match", true)]
    [InlineData("MissingComponentsRow", false)]
    [InlineData("MissingIconsRow", false)]
    [InlineData("StaleComponentsRow", false)]
    [InlineData("StaleIconsRow", false)]
    [InlineData("DuplicateComponentsRow", false)]
    [InlineData("DuplicateIconsRow", false)]
    [InlineData("MissingComponentsCatalog", false)]
    [InlineData("MissingIconsCatalog", false)]
    [InlineData("DuplicateComponentsCatalog", false)]
    [InlineData("DuplicateIconsCatalog", false)]
    [InlineData("UnresolvedComponentsVersion", false)]
    [InlineData("UnresolvedIconsVersion", false)]
    [InlineData("UnresolvedDocumentVersion", false)]
    [InlineData("FloatingVersion", false)]
    [InlineData("ConditionalVersion", false)]
    [InlineData("MalformedXml", false)]
    [InlineData("MissingGitlink", false)]
    [InlineData("MissingObject", false)]
    [InlineData("MissingBuildsRepository", false)]
    [InlineData("MissingPage", false)]
    [InlineData("FencedTable", false)]
    [InlineData("SplitTables", false)]
    [InlineData("AdvancedCheckout", true)]
    [InlineData("DirtyCheckout", true)]
    public async Task FluentCatalogParityUsesRootHeadSelectedObjectsAsync(string scenario, bool passes)
    {
        await AssertFluentCatalogParityAsync(scenario, passes);
    }

    [Theory]
    [InlineData("AmbiguousItemUpdate", false)]
    [InlineData("UnrelatedLiteralEntries", true)]
    [InlineData("HtmlPreTable", false)]
    [InlineData("UnclosedHtmlPreTable", false)]
    [InlineData("UppercaseHtmlPreTable", false)]
    [InlineData("FencedPreExample", true)]
    public async Task FluentCatalogParityCoversReviewRegressionsAsync(string scenario, bool passes)
    {
        await AssertFluentCatalogParityAsync(scenario, passes);
    }

    /// <summary>
    /// Verifies exclusions cannot supply a Fluent identity while unrelated items remain permitted.
    /// </summary>
    /// <param name="scenario">The catalog fixture to validate.</param>
    /// <param name="passes">Whether the fixture should pass identity validation.</param>
    [Theory]
    [InlineData("ExcludedComponentsCatalog", false)]
    [InlineData("ExcludedIconsCatalog", false)]
    [InlineData("UnresolvedExclude", false)]
    [InlineData("EmptyExclude", false)]
    [InlineData("UnrelatedExclude", false)]
    [InlineData("UnrelatedExcludedPackage", true)]
    public async Task FluentCatalogParityHandlesExcludedItemsAsync(string scenario, bool passes)
    {
        await AssertFluentCatalogParityAsync(scenario, passes);
    }

    private static async Task AssertFluentCatalogParityAsync(string scenario, bool passes)
    {
        string root = NewFluentFixtureRoot();
        try
        {
            const string components = "Microsoft.FluentUI.AspNetCore.Components";
            const string icons = components + ".Icons";
            string catalog = FluentCatalog("7.8.9", "8.9.0");
            string page = "| Package ID | Selected version |\n| --- | --- |\n" +
                $"| `{components}` | `7.8.9` |\n| `{icons}` | `8.9.0` |\n";
            string componentRow = $"| `{components}` | `7.8.9` |\n";
            string iconsRow = $"| `{icons}` | `8.9.0` |\n";
            switch (scenario)
            {
                case "MissingComponentsRow": page = page.Replace(componentRow, "", StringComparison.Ordinal); break;
                case "MissingIconsRow": page = page.Replace(iconsRow, "", StringComparison.Ordinal); break;
                case "StaleComponentsRow": page = page.Replace("7.8.9", "7.8.8", StringComparison.Ordinal); break;
                case "StaleIconsRow": page = page.Replace("8.9.0", "8.8.9", StringComparison.Ordinal); break;
                case "DuplicateComponentsRow": page += componentRow; break;
                case "DuplicateIconsRow": page += iconsRow; break;
                case "MissingComponentsCatalog": catalog = catalog.Replace($"<PackageVersion Include=\"{components}\" Version=\"7.8.9\" />", "", StringComparison.Ordinal); break;
                case "MissingIconsCatalog": catalog = catalog.Replace($"<PackageVersion Include=\"{icons}\" Version=\"8.9.0\" />", "", StringComparison.Ordinal); break;
                case "DuplicateComponentsCatalog": catalog = catalog.Replace("</ItemGroup>", $"<PackageVersion Include=\"{components}\" Version=\"7.8.9\" /></ItemGroup>", StringComparison.Ordinal); break;
                case "DuplicateIconsCatalog": catalog = catalog.Replace("</ItemGroup>", $"<PackageVersion Include=\"{icons}\" Version=\"8.9.0\" /></ItemGroup>", StringComparison.Ordinal); break;
                case "UnresolvedComponentsVersion": catalog = catalog.Replace("7.8.9", "$(FluentVersion)", StringComparison.Ordinal); break;
                case "UnresolvedIconsVersion": catalog = catalog.Replace("8.9.0", "$(FluentVersion)", StringComparison.Ordinal); break;
                case "UnresolvedDocumentVersion": page = page.Replace("7.8.9", "$(FluentVersion)", StringComparison.Ordinal); break;
                case "FloatingVersion": catalog = catalog.Replace("7.8.9", "7.*", StringComparison.Ordinal); break;
                case "ConditionalVersion": catalog = catalog.Replace("<ItemGroup>", "<ItemGroup Condition=\"'$(TargetFramework)' == 'net10.0'\">", StringComparison.Ordinal); break;
                case "AmbiguousItemUpdate": catalog = catalog.Replace("</ItemGroup>", "<PackageVersion Update=\"@(PackageVersion)\" Version=\"99.0.0\" /></ItemGroup>", StringComparison.Ordinal); break;
                case "UnrelatedLiteralEntries": catalog = catalog.Replace("</ItemGroup>", "<PackageVersion Include=\"Unrelated.Package\" Version=\"$(OtherVersion)\" /><PackageVersion Update=\"Unrelated.Package\" Version=\"99.0.0\" /></ItemGroup>", StringComparison.Ordinal); break;
                case "ExcludedComponentsCatalog": catalog = catalog.Replace($"Include=\"{components}\"", $"Include=\"{components}\" Exclude=\"{components}\"", StringComparison.Ordinal); break;
                case "ExcludedIconsCatalog": catalog = catalog.Replace($"Include=\"{icons}\"", $"Include=\"{icons}\" Exclude=\"{icons}\"", StringComparison.Ordinal); break;
                case "UnresolvedExclude": catalog = catalog.Replace($"Include=\"{components}\"", $"Include=\"{components}\" Exclude=\"$(ExcludedPackages)\"", StringComparison.Ordinal); break;
                case "EmptyExclude": catalog = catalog.Replace($"Include=\"{components}\"", $"Include=\"{components}\" Exclude=\"\"", StringComparison.Ordinal); break;
                case "UnrelatedExclude": catalog = catalog.Replace($"Include=\"{components}\"", $"Include=\"{components}\" Exclude=\"Unrelated.Package\"", StringComparison.Ordinal); break;
                case "UnrelatedExcludedPackage": catalog = catalog.Replace("</ItemGroup>", "<PackageVersion Include=\"Unrelated.Package\" Exclude=\"Unrelated.Package\" Version=\"99.0.0\" /></ItemGroup>", StringComparison.Ordinal); break;
                case "MalformedXml": catalog = "<Project>"; break;
                case "FencedTable": page = "```markdown\n" + page + "```\n"; break;
                case "HtmlPreTable": page = "<pre>\n" + page + "</pre>\n"; break;
                case "UnclosedHtmlPreTable": page = "<pre>\n" + page; break;
                case "UppercaseHtmlPreTable": page = "<PRE class=\"example\">\n" + page + "</PRE>\n"; break;
                case "FencedPreExample": page = "```html\n<pre>\n```\n\n" + page; break;
                case "SplitTables": page = page.Replace(iconsRow, "\n| Package ID | Selected version |\n| --- | --- |\n" + iconsRow, StringComparison.Ordinal); break;
            }
            _ = Directory.CreateDirectory(Path.Combine(root, "docs"));
            string pagePath = Path.Combine(root, "docs", "fluent-ui-v5-contingency.md");
            if (scenario != "MissingPage") { await File.WriteAllTextAsync(pagePath, page, CancellationToken.None).ConfigureAwait(false); }
            await SeedFluentAuthorityAsync(root, catalog, scenario == "MissingGitlink", scenario == "MissingObject").ConfigureAwait(false);
            string builds = Path.Combine(root, "references", "Hexalith.Builds");
            if (scenario is "AdvancedCheckout" or "DirtyCheckout")
            {
                await File.WriteAllTextAsync(Path.Combine(builds, "Props", "Directory.Packages.props"), FluentCatalog("99.0.0", "99.0.0"), CancellationToken.None).ConfigureAwait(false);
                if (scenario == "AdvancedCheckout") { await CommitFluentFixtureAsync(builds).ConfigureAwait(false); }
            }
            if (scenario == "MissingBuildsRepository") { Directory.Delete(Path.Combine(builds, ".git"), recursive: true); }

            string[] failures = await RunFluentParityAsync(root).ConfigureAwait(false);

            if (passes) { failures.ShouldBeEmpty(); }
            else { failures.ShouldNotBeEmpty(scenario); }
            failures.ShouldAllBe(failure => failure.StartsWith("docs/fluent-ui-v5-contingency.md:", StringComparison.Ordinal) || failure.StartsWith("references/Hexalith.Builds", StringComparison.Ordinal));
            failures.ShouldAllBe(failure => failure.Length < 260);
            string.Join('\n', failures).ShouldNotContain(root);
            // Deterministic diagnostics do not contain Git stderr, XML contents or random fixture paths.
            (await RunFluentParityAsync(root).ConfigureAwait(false)).ShouldBe(failures);
        }
        finally { DeleteFluentFixture(root); }
    }

    [Theory]
    [InlineData("Match")]
    [InlineData("ComponentsDrift")]
    [InlineData("IconsDrift")]
    [InlineData("MissingToc")]
    [InlineData("MissingDocfx")]
    [InlineData("MissingMetadata")]
    public async Task FluentContingencyPublicationCommandChecksIdentityAndWiringAsync(string scenario)
    {
        await AssertFluentPublicationAsync(scenario);
    }

    [Theory]
    [InlineData("RecursiveMarkdownExclusion")]
    [InlineData("UnrelatedDocfxExclusion")]
    [InlineData("TocDescriptionScalar")]
    [InlineData("TocItemNameScalar")]
    public async Task FluentContingencyPublicationCoversReviewRegressionsAsync(string scenario)
    {
        await AssertFluentPublicationAsync(scenario);
    }

    private static async Task AssertFluentPublicationAsync(string scenario)
    {
        string root = NewFluentFixtureRoot();
        try
        {
            foreach (string relative in new[]
            {
                "eng/validate-docs.ps1", "global.json", ".gitmodules", ".config/dotnet-tools.json", "docs",
                "src/Hexalith.FrontComposer.Cli/MigrationCatalog.cs",
                "src/Hexalith.FrontComposer.Shell/Components",
                "src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionStatus.cs",
                "src/Hexalith.FrontComposer.Contracts", "src/Hexalith.FrontComposer.Contracts.UI/Rendering",
                "_bmad-output/project-docs/component-inventory.md",
                "_bmad-output/contracts/fc-doc-component-documentation-2026-06-03.md",
            })
            {
                CopyFluentPublicationInput(Path.Combine(ProjectRoot(), relative), Path.Combine(root, relative));
            }
            string selection = await RunFluentGitAsync(ProjectRoot(), "ls-tree", "HEAD", "--", "references/Hexalith.Builds").ConfigureAwait(false);
            string selected = selection.Split(' ', '\t')[2];
            string catalog = await RunFluentGitAsync(Path.Combine(ProjectRoot(), "references", "Hexalith.Builds"), "show", selected + ":Props/Directory.Packages.props").ConfigureAwait(false);
            await SeedFluentAuthorityAsync(root, catalog).ConfigureAwait(false);
            string page = Path.Combine(root, "docs", "fluent-ui-v5-contingency.md");
            if (scenario is "ComponentsDrift" or "IconsDrift")
            {
                string package = "Microsoft.FluentUI.AspNetCore.Components" + (scenario == "IconsDrift" ? ".Icons" : "");
                string text = File.ReadAllText(page);
                string drifted = Regex.Replace(text, @"(?m)^(\| `" + Regex.Escape(package) + @"` \| )`[^`]+`", "$1`99.0.0`");
                drifted.ShouldNotBe(text);
                File.WriteAllText(page, drifted);
            }
            if (scenario == "MissingToc")
            {
                string toc = Path.Combine(root, "docs", "toc.yml");
                File.WriteAllText(toc, File.ReadAllText(toc).Replace("    href: fluent-ui-v5-contingency.md", "    # href: fluent-ui-v5-contingency.md", StringComparison.Ordinal));
            }
            if (scenario is "TocDescriptionScalar" or "TocItemNameScalar")
            {
                string toc = Path.Combine(root, "docs", "toc.yml");
                string text = File.ReadAllText(toc).Replace("\r\n", "\n", StringComparison.Ordinal);
                if (scenario == "TocDescriptionScalar")
                {
                    text = text.Replace("  - name: Fluent UI v5 contingency\n    href: fluent-ui-v5-contingency.md\n", "", StringComparison.Ordinal)
                        .Replace("  href: how-to/index.md\n", "  href: how-to/index.md\n  description: |\n    href: fluent-ui-v5-contingency.md\n", StringComparison.Ordinal);
                }
                else
                {
                    text = text.Replace("  - name: Fluent UI v5 contingency", "  - name: |", StringComparison.Ordinal);
                }
                File.WriteAllText(toc, text);
            }
            if (scenario is "RecursiveMarkdownExclusion" or "UnrelatedDocfxExclusion")
            {
                string config = Path.Combine(root, "docs", "docfx.json");
                System.Text.Json.Nodes.JsonNode json = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(config))!;
                json["build"]!["content"]![0]!["exclude"] = JsonSerializer.SerializeToNode(new[] { scenario == "RecursiveMarkdownExclusion" ? "**/*.md" : "reference/**/*.md" });
                File.WriteAllText(config, json.ToJsonString());
            }
            if (scenario == "MissingDocfx")
            {
                string config = Path.Combine(root, "docs", "docfx.json");
                File.WriteAllText(config, File.ReadAllText(config).Replace("\"fluent-ui-v5-contingency.md\",", "", StringComparison.Ordinal));
            }
            if (scenario == "MissingMetadata")
            {
                File.WriteAllText(page, Regex.Replace(File.ReadAllText(page), @"(?m)^reviewed:[^\n]*\n", ""));
            }
            (int exitCode, string output, string error) = await RunFluentProcessAsync(root, "pwsh", "-NoProfile", "-File", "eng/validate-docs.ps1", "-SkipDocFx", "-SkipSnippetBuild").ConfigureAwait(false);
            using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "artifacts", "docs", "validation-manifest.json")));
            manifest.RootElement.GetProperty("inputRoots").EnumerateArray().Select(value => value.GetString())
                .ShouldContain("docs/fluent-ui-v5-contingency.md");
            string[] failures = manifest.RootElement.GetProperty("blockingFailures").EnumerateArray().Select(value => value.GetString()!).ToArray();
            if (scenario is "Match" or "UnrelatedDocfxExclusion")
            {
                exitCode.ShouldBe(0, output + error);
                failures.ShouldBeEmpty();
            }
            else
            {
                exitCode.ShouldNotBe(0, output + error);
                failures.Length.ShouldBe(1);
                string expected = scenario switch
                {
                    "MissingToc" or "TocDescriptionScalar" or "TocItemNameScalar" => "docs/toc.yml: missing Fluent contingency page under How-to.",
                    "MissingDocfx" or "RecursiveMarkdownExclusion" => "docs/docfx.json: must publish the Fluent contingency page at its documentation path.",
                    "MissingMetadata" => "docs/fluent-ui-v5-contingency.md is missing required front matter 'reviewed'.",
                    _ => "docs/fluent-ui-v5-contingency.md: version for Microsoft.FluentUI.AspNetCore.Components" + (scenario == "IconsDrift" ? ".Icons" : "") + " must match the root HEAD-selected catalog.",
                };
                failures[0].ShouldBe(expected);
                failures[0].ShouldNotContain(root);
            }
        }
        finally { DeleteFluentFixture(root); }
    }

    private static string FluentCatalog(string components, string icons) => $"""
        <Project><ItemGroup>
          <PackageVersion Include="Microsoft.FluentUI.AspNetCore.Components" Version="{components}" />
          <PackageVersion Include="Microsoft.FluentUI.AspNetCore.Components.Icons" Version="{icons}" />
        </ItemGroup></Project>
        """;

    private static string NewFluentFixtureRoot()
    {
        string root = Path.Combine(Path.GetTempPath(), "hfc-fluent-doc-tests", Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(root);
        return root;
    }

    private static async Task SeedFluentAuthorityAsync(string root, string catalog, bool missingGitlink = false, bool missingObject = false)
    {
        string builds = Path.Combine(root, "references", "Hexalith.Builds");
        _ = Directory.CreateDirectory(Path.Combine(builds, "Props"));
        await RunFluentGitAsync(builds, "init", "--quiet").ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(builds, "Props", "Directory.Packages.props"), catalog, CancellationToken.None).ConfigureAwait(false);
        await CommitFluentFixtureAsync(builds).ConfigureAwait(false);
        string selected = (await RunFluentGitAsync(builds, "rev-parse", "HEAD").ConfigureAwait(false)).Trim();
        await RunFluentGitAsync(root, "init", "--quiet").ConfigureAwait(false);
        if (!missingGitlink)
        {
            await RunFluentGitAsync(root, "update-index", "--add", "--cacheinfo", "160000," + (missingObject ? new string('1', 40) : selected) + ",references/Hexalith.Builds").ConfigureAwait(false);
        }
        await RunFluentGitAsync(root, "-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid", "-c", "commit.gpgsign=false", "-c", "core.hooksPath=", "commit", "--quiet", "--allow-empty", "-m", "test: seed catalog fixture").ConfigureAwait(false);
    }

    private static async Task CommitFluentFixtureAsync(string root)
    {
        await RunFluentGitAsync(root, "add", "Props/Directory.Packages.props").ConfigureAwait(false);
        await RunFluentGitAsync(root, "-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid", "-c", "commit.gpgsign=false", "-c", "core.hooksPath=", "commit", "--quiet", "-m", "test: seed catalog fixture").ConfigureAwait(false);
    }

    private static async Task<string[]> RunFluentParityAsync(string root)
    {
        string runner = Path.Combine(root, "fluent-parity.ps1");
        await File.WriteAllTextAsync(runner, """
            param([string]$Validator, [string]$FixtureRoot)
            $ErrorActionPreference = 'Stop'
            Set-StrictMode -Version Latest
            $tokens = $null; $errors = $null
            $ast = [System.Management.Automation.Language.Parser]::ParseFile($Validator, [ref]$tokens, [ref]$errors)
            if ($errors.Count) { throw 'Publication validator has parse errors.' }
            $names = @('Add-Failure', 'Get-FrontMatter', 'Invoke-FluentCatalogGit', 'Get-FluentCatalogVersions', 'Assert-FluentContingencyParity')
            foreach ($function in $ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] }, $false)) {
                if ($function.Name -in $names) { . ([scriptblock]::Create($function.Extent.Text)) }
            }
            $failures = [System.Collections.Generic.List[string]]::new()
            Assert-FluentContingencyParity $FixtureRoot $failures
            ConvertTo-Json -InputObject $failures.ToArray() -Compress
            """, CancellationToken.None).ConfigureAwait(false);
        (int exitCode, string output, string error) = await RunFluentProcessAsync(root, "pwsh", "-NoProfile", "-File", runner, Path.Combine(ProjectRoot(), "eng", "validate-docs.ps1"), root).ConfigureAwait(false);
        exitCode.ShouldBe(0, error + output);
        return JsonSerializer.Deserialize<string[]>(output)!;
    }

    private static async Task<string> RunFluentGitAsync(string root, params string[] arguments)
    {
        (int exitCode, string output, string error) = await RunFluentProcessAsync(root, "git", arguments).ConfigureAwait(false);
        exitCode.ShouldBe(0, error + output);
        return output;
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunFluentProcessAsync(string root, string fileName, params string[] arguments)
    {
        ProcessStartInfo start = new(fileName)
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string argument in arguments) { start.ArgumentList.Add(argument); }
        using Process process = Process.Start(start)!;
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(90));
        try { await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false); }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await Task.WhenAll(output, error).ConfigureAwait(false);
            throw new TimeoutException(fileName + " did not exit within 90 seconds.");
        }
        return (process.ExitCode, await output.ConfigureAwait(false), await error.ConfigureAwait(false));
    }

    private static void CopyFluentPublicationInput(string source, string destination)
    {
        if (File.Exists(source))
        {
            _ = Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(source, destination);
            return;
        }
        _ = Directory.CreateDirectory(destination);
        foreach (string file in Directory.EnumerateFiles(source)) { File.Copy(file, Path.Combine(destination, Path.GetFileName(file))); }
        foreach (string directory in Directory.EnumerateDirectories(source))
        {
            if (Path.GetFileName(directory) is "bin" or "obj" or "_site") { continue; }
            CopyFluentPublicationInput(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
    }

    private static void DeleteFluentFixture(string root)
    {
        foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)) { File.SetAttributes(file, FileAttributes.Normal); }
        Directory.Delete(root, recursive: true);
    }

    [GeneratedRegex("(?s)<!--\\s*hfc:reference:start\\s*-->(.*?)<!--\\s*hfc:reference:end\\s*-->")]
    private static partial Regex HfcReferenceRegion();

    private static IEnumerable<string> ContentFiles() {
        string docs = Path.Combine(ProjectRoot(), "docs");
        string[] roots = ["tutorials", "how-to", "reference", "concepts", "migrations"];

        yield return Path.Combine(docs, "index.md");
        yield return Path.Combine(docs, "fluent-ui-v5-contingency.md");
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

    private static string NormalizeWhitespace(string value)
        => Regex.Replace(value, @"\s+", " ").Trim();

    private static string Canonical(string value)
        => Regex.Replace(Uri.UnescapeDataString(value).Normalize().Trim().TrimEnd('/').ToLowerInvariant(), @"[\\/_\-\.\s]+", "");

    private static string Relative(string path)
        => Path.GetRelativePath(ProjectRoot(), path).Replace('\\', '/');

    private static string ProjectRoot() {
        DirectoryInfo directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.FrontComposer.slnx"))) {
            directory = directory.Parent!;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate project root.");
    }
}
