using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.SourceTools.Diagnostics;

using Microsoft.CodeAnalysis;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Diagnostics;

/// <summary>
/// Story 9-4 registry contract tests. The registry is the single source of truth for HFC
/// ownership, lifecycle, docs links, release rows, and channel severity metadata.
/// </summary>
[Trait("Category", "Governance")]
public sealed partial class DiagnosticRegistryTests {
    private const string SupportedSchemaVersion = "1.0";
    private const string CanonicalHelpLinkFormat = "https://hexalith.github.io/FrontComposer/diagnostics/{0}";
    private static readonly StringComparer Ordinal = StringComparer.Ordinal;

    [Fact]
    public void RegistryContract_IsVersionedSortedUniqueAndRangeOwned() {
        DiagnosticRegistry registry = LoadRegistry();

        registry.SchemaVersion.ShouldBe(SupportedSchemaVersion);
        registry.CanonicalHelpLinkFormat.ShouldBe(CanonicalHelpLinkFormat);
        registry.Ranges.Select(r => r.OwnerPackage).ShouldBe([
            "Contracts",
            "SourceTools",
            "Shell",
            "EventStore",
            "Mcp",
            "Aspire",
        ], ignoreOrder: false);

        string[] ids = registry.Diagnostics.Select(d => d.Id).ToArray();
        ids.ShouldBe(ids.OrderBy(id => id, Ordinal).ToArray());
        ids.ShouldBeUnique();
        registry.Diagnostics.Select(d => d.DocsSlug).ShouldBeUnique();
        registry.Diagnostics.Select(d => d.Title).ShouldBeUnique();

        foreach (DiagnosticEntry diagnostic in registry.Diagnostics) {
            diagnostic.Id.ShouldMatch("^HFC[0-9]{4}$");
            diagnostic.Lifecycle.ShouldBeOneOf("reserved", "active", "deprecated", "retired", "removed-in-major");
            diagnostic.OwnerPackage.ShouldNotBeNullOrWhiteSpace();
            diagnostic.OwnerStory.ShouldNotBeNullOrWhiteSpace();
            diagnostic.ReleaseRow.ShouldNotBeNullOrWhiteSpace();
            diagnostic.MessageTemplate.ShouldContain("What:", Case.Insensitive);
            diagnostic.MessageTemplate.ShouldContain("Expected:", Case.Insensitive);
            diagnostic.MessageTemplate.ShouldContain("Fix:", Case.Insensitive);
            diagnostic.DocsSlug.ShouldBe($"diagnostics/{diagnostic.Id}");
            diagnostic.HelpLinkUri.ShouldBe(string.Format(CanonicalHelpLinkFormat, diagnostic.Id));
            diagnostic.RedactionClass.ShouldNotBeNullOrWhiteSpace();
            diagnostic.SuppressionPolicy.ShouldNotBeNullOrWhiteSpace();

            DiagnosticRange range = registry.Ranges.Single(r => r.OwnerPackage == diagnostic.OwnerPackage);
            int numericId = int.Parse(diagnostic.Id[3..], System.Globalization.CultureInfo.InvariantCulture);
            numericId.ShouldBeInRange(range.Start, range.End, $"{diagnostic.Id} is outside {diagnostic.OwnerPackage} range.");
        }
    }

    [Fact]
    public void Registry_CoversDescriptorConstantsAndReleaseRows() {
        DiagnosticRegistry registry = LoadRegistry();
        Dictionary<string, DiagnosticEntry> byId = registry.Diagnostics.ToDictionary(d => d.Id, Ordinal);
        HashSet<string> constantIds = TypeIdConstants(typeof(FcDiagnosticIds)).ToHashSet(Ordinal);
        HashSet<string> descriptorIds = DiagnosticDescriptorFields().Select(d => d.Id).ToHashSet(Ordinal);
        HashSet<string> releaseRows = ReleaseRows().ToHashSet(Ordinal);

        foreach (string id in constantIds.Concat(descriptorIds).Distinct(Ordinal)) {
            byId.ShouldContainKey(id, $"{id} must have a diagnostic-registry row.");
        }

        foreach (DiagnosticDescriptor descriptor in DiagnosticDescriptorFields()) {
            DiagnosticEntry entry = byId[descriptor.Id];
            entry.CompilerSeverity.ShouldBe(descriptor.DefaultSeverity.ToString());
            descriptor.Category.ShouldBe("HexalithFrontComposer");
            descriptor.Title.ToString().ShouldBe(entry.Title);
            descriptor.HelpLinkUri.ShouldBe(entry.HelpLinkUri);
            releaseRows.ShouldContain(descriptor.Id, $"{descriptor.Id} must be listed in AnalyzerReleases.");
        }

        foreach (string releaseRow in releaseRows.Where(id => id.StartsWith("HFC", StringComparison.Ordinal))) {
            byId.ShouldContainKey(releaseRow, $"{releaseRow} release row must resolve through the registry.");
        }
    }

    [Fact]
    public void DocsStubs_ArePresentBoundedAndRegistryBacked() {
        DiagnosticRegistry registry = LoadRegistry();
        DirectoryInfo docsRoot = DiagnosticsDocsRoot();

        foreach (DiagnosticEntry diagnostic in registry.Diagnostics) {
            FileInfo stub = new(Path.Combine(docsRoot.FullName, $"{diagnostic.Id}.md"));
            stub.Exists.ShouldBeTrue($"{diagnostic.Id} docs stub is missing.");

            string markdown = File.ReadAllText(stub.FullName);
            markdown.ShouldStartWith("---");
            markdown.ShouldContain($"id: {diagnostic.Id}");
            markdown.ShouldContain($"title: \"{EscapeYamlScalar(diagnostic.Title)}\"");
            markdown.ShouldContain($"ownerPackage: {diagnostic.OwnerPackage}");
            markdown.ShouldContain($"lifecycle: {diagnostic.Lifecycle}");
            markdown.ShouldContain($"docsSlug: {diagnostic.DocsSlug}");
            markdown.ShouldContain("<!-- story-9-5:narrative-start -->");
            markdown.ShouldContain("## Problem");
            markdown.ShouldContain("## Common Causes");
            markdown.ShouldContain("## How To Fix");
            markdown.ShouldContain("## Example");
            markdown.ShouldContain("## Suppression Guidance");
            markdown.ShouldContain("## Migration/Deprecation");
            markdown.ShouldContain("## Related Diagnostics");
            markdown.ShouldNotContain("<script", Case.Insensitive);
            markdown.ShouldNotContain("`=cmd", Case.Insensitive);
        }

        string[] orphanStubs = Directory.EnumerateFiles(docsRoot.FullName, "HFC*.md")
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Except(registry.Diagnostics.Select(d => d.Id), Ordinal)
            .ToArray();
        orphanStubs.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("2.0", "unsupported-schema")]
    [InlineData("1.0", "duplicate-id")]
    public void RegistryValidator_FailsClosedWithNamedCategories(string schemaVersion, string expectedCategory) {
        DiagnosticRegistry registry = LoadRegistry();
        JsonObject json = RegistryJson().DeepClone().AsObject();
        json["schemaVersion"] = schemaVersion;

        if (expectedCategory == "duplicate-id") {
            JsonArray diagnostics = json["diagnostics"]!.AsArray();
            diagnostics.Add(diagnostics[0]!.DeepClone());
        }

        string[] categories = ValidateRegistryJson(json).ToArray();
        categories.ShouldContain(expectedCategory);
    }

    [Theory]
    [InlineData("diagnostics/HFC1058")]
    [InlineData("diagnostics/hfc1058")]
    [InlineData("diagnostics/HFC1058%2fescape")]
    [InlineData("../diagnostics/HFC1058")]
    [InlineData("diagnostics/HFC1058?x=1")]
    [InlineData("diagnostics/HFC1058\u200B")]
    public void DocsSlugValidation_DistinguishesUnsafeCanonicalizationFailures(string docsSlug) {
        JsonObject json = RegistryJson().DeepClone().AsObject();
        JsonObject diagnostic = json["diagnostics"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(node => node["id"]!.GetValue<string>() == "HFC1058");
        diagnostic["docsSlug"] = docsSlug;

        string[] categories = ValidateRegistryJson(json).ToArray();

        if (docsSlug == "diagnostics/HFC1058") {
            categories.ShouldNotContain("invalid-slug");
            return;
        }

        categories.ShouldContain("invalid-slug");
    }

    [Fact]
    public void FrontComposerObsoleteAttributes_FollowDiagnosticDeprecationPolicy() {
        DiagnosticRegistry registry = LoadRegistry();
        HashSet<string> ids = registry.Diagnostics.Select(d => d.Id).ToHashSet(Ordinal);
        string[] sourceFiles = Directory.EnumerateFiles(ProjectRoot().FullName, "*.cs", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}src{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();

        Regex obsoleteRegex = ObsoleteAttributeRegex();
        foreach (string file in sourceFiles) {
            string text = File.ReadAllText(file);
            foreach (Match match in obsoleteRegex.Matches(text)) {
                string message = match.Groups["message"].Value;
                message.ShouldMatch(@"^(?<old>.+?) (replaced by .+?|has no direct replacement) in v(?<target>\d+\.\d+(?:[-.a-zA-Z0-9]+)?). See (?<id>HFC\d{4}). Removed in v(?<removal>\d+\.\d+(?:[-.a-zA-Z0-9]+)?)\.$", $"{file} obsolete message must follow Story 9-4 policy.");
                string id = HfcIdRegex().Match(message).Value;
                ids.ShouldContain(id);
            }
        }
    }

    [Fact]
    public void CompatibilitySuppressionEvidence_IsDeterministicAndBounded() {
        FileInfo suppression = new(Path.Combine(ProjectRoot().FullName, "docs", "diagnostics", "compatibility-suppressions.json"));
        suppression.Exists.ShouldBeTrue("Story 9-4 compatibility evidence file is missing.");

        JsonObject json = JsonNode.Parse(File.ReadAllText(suppression.FullName))!.AsObject();
        json["schemaVersion"]!.GetValue<string>().ShouldBe("1.0");
        JsonArray suppressions = json["suppressions"]!.AsArray();
        foreach (JsonNode? node in suppressions) {
            JsonObject item = node!.AsObject();
            item["package"]!.GetValue<string>().ShouldStartWith("Hexalith.FrontComposer.");
            item["tfm"]!.GetValue<string>().ShouldNotBeNullOrWhiteSpace();
            item["oldSignature"]!.GetValue<string>().ShouldNotBeNullOrWhiteSpace();
            item["newState"]!.GetValue<string>().ShouldNotBeNullOrWhiteSpace();
            item["hfcId"]!.GetValue<string>().ShouldMatch("^HFC[0-9]{4}$");
            item["targetRelease"]!.GetValue<string>().ShouldMatch("^v[0-9]+\\.[0-9]+");
            item["reviewerRationale"]!.GetValue<string>().Length.ShouldBeInRange(16, 400);
        }
    }

    [Fact]
    public void SubmoduleBoundaries_AreDocumentedAndExcludedFromRegistryOwnershipScan() {
        FileInfo gitmodules = new(Path.Combine(ProjectRoot().FullName, ".gitmodules"));
        gitmodules.Exists.ShouldBeTrue();
        string modules = File.ReadAllText(gitmodules.FullName);
        modules.ShouldContain("Hexalith.EventStore");
        modules.ShouldContain("Hexalith.Tenants");

        DiagnosticRegistry registry = LoadRegistry();
        registry.ExternalBoundaries.ShouldContain("Hexalith.EventStore");
        registry.ExternalBoundaries.ShouldContain("Hexalith.Tenants");
        registry.Diagnostics.ShouldNotContain(d => d.OwnerPackage == "EventStore" && d.OwnerStory == "external-submodule");
    }

    [Fact]
    public void SourceHfcIds_AreRegistryBackedAndDiagnosticLinksUseCanonicalHost() {
        DiagnosticRegistry registry = LoadRegistry();
        HashSet<string> registryIds = registry.Diagnostics.Select(d => d.Id).ToHashSet(Ordinal);
        HashSet<string> rangeMarkers = ["HFC1000", "HFC1099", "HFC1999"];

        foreach (string file in SourceFiles()) {
            string text = File.ReadAllText(file);
            text.ShouldNotContain("https://hexalith.dev/frontcomposer/diagnostics/", Case.Insensitive);
            text.ShouldNotContain("https://hexalith.io/docs/policies/", Case.Insensitive);

            foreach (Match match in HfcIdRegex().Matches(text)) {
                if (rangeMarkers.Contains(match.Value)) {
                    continue;
                }

                registryIds.ShouldContain(match.Value, $"{ProjectRelativePath(file)} references {match.Value}, which must resolve through the registry.");
            }
        }
    }

    [Fact]
    public void PackableProjects_UsePackageValidationBaselinePolicy() {
        string directoryBuildProps = File.ReadAllText(Path.Combine(ProjectRoot().FullName, "Directory.Build.props"));
        directoryBuildProps.ShouldContain("<EnablePackageValidation>true</EnablePackageValidation>");
        directoryBuildProps.ShouldContain("<PackageValidationBaselineVersion>0.1.0</PackageValidationBaselineVersion>");
        directoryBuildProps.ShouldContain("<ApiCompatGenerateSuppressionFile>false</ApiCompatGenerateSuppressionFile>");

        string[] packableProjects = Directory.EnumerateFiles(Path.Combine(ProjectRoot().FullName, "src"), "*.csproj", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("<IsPackable>true</IsPackable>", StringComparison.Ordinal))
            .Select(ProjectRelativePath)
            .OrderBy(path => path, Ordinal)
            .ToArray();

        packableProjects.ShouldBe([
            "src/Hexalith.FrontComposer.Mcp/Hexalith.FrontComposer.Mcp.csproj",
            "src/Hexalith.FrontComposer.Schema/Hexalith.FrontComposer.Schema.csproj",
        ], ignoreOrder: false);
    }

    [Fact]
    public void DriftSampleReports_AreNormalizedAndCommitted() {
        string samplesRoot = Path.Combine(DiagnosticsDocsRoot().FullName, "samples");
        string[] expectedSamples = [
            "compatibility-drift-report.json",
            "docs-stub-drift-report.json",
            "registry-drift-report.json",
            "release-row-drift-report.json",
        ];

        Directory.EnumerateFiles(samplesRoot, "*.json")
            .Select(Path.GetFileName)
            .OrderBy(name => name, Ordinal)
            .ShouldBe(expectedSamples, ignoreOrder: false);

        foreach (string sample in expectedSamples) {
            string path = Path.Combine(samplesRoot, sample);
            string json = File.ReadAllText(path);
            JsonObject parsed = JsonNode.Parse(json)!.AsObject();
            parsed["schemaVersion"]!.GetValue<string>().ShouldBe("1.0");
            parsed["exitCode"]!.GetValue<int>().ShouldBe(2);
            json.ShouldNotContain(DateTimeOffset.Now.Year.ToString(System.Globalization.CultureInfo.InvariantCulture));
            json.ShouldNotContain(ProjectRoot().FullName.Replace('\\', '/'), Case.Insensitive);
            json.ShouldNotContain(Environment.MachineName, Case.Insensitive);
            json.ShouldNotContain("https://api.github.com", Case.Insensitive);
            json.ShouldNotContain("https://api.nuget.org", Case.Insensitive);
        }
    }

    private static IEnumerable<string> ValidateRegistryJson(JsonObject json) {
        if (json["schemaVersion"]?.GetValue<string>() != SupportedSchemaVersion) {
            yield return "unsupported-schema";
        }

        HashSet<string> ids = new(Ordinal);
        HashSet<string> slugs = new(Ordinal);
        foreach (JsonNode? node in json["diagnostics"]!.AsArray()) {
            JsonObject diagnostic = node!.AsObject();
            string id = diagnostic["id"]!.GetValue<string>();
            string slug = diagnostic["docsSlug"]!.GetValue<string>();
            if (!ids.Add(id)) {
                yield return "duplicate-id";
            }

            if (!slugs.Add(slug)) {
                yield return "duplicate-slug";
            }

            if (slug != $"diagnostics/{id}" || slug.Contains('\\', StringComparison.Ordinal) || slug.Contains('%', StringComparison.Ordinal) || slug.Contains('?', StringComparison.Ordinal) || slug.Contains("..", StringComparison.Ordinal) || slug.Any(char.IsWhiteSpace) || slug.Any(IsZeroWidth)) {
                yield return "invalid-slug";
            }
        }
    }

    private static bool IsZeroWidth(char value)
        => value is '\u200B' or '\u200C' or '\u200D' or '\uFEFF';

    private static DiagnosticRegistry LoadRegistry()
        => RegistryJson().Deserialize<DiagnosticRegistry>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    private static JsonObject RegistryJson() {
        FileInfo registry = new(Path.Combine(DiagnosticsDocsRoot().FullName, "diagnostic-registry.json"));
        registry.Exists.ShouldBeTrue("docs/diagnostics/diagnostic-registry.json is missing.");
        return JsonNode.Parse(File.ReadAllText(registry.FullName))!.AsObject();
    }

    private static DirectoryInfo DiagnosticsDocsRoot()
        => new(Path.Combine(ProjectRoot().FullName, "docs", "diagnostics"));

    private static DirectoryInfo ProjectRoot() {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Hexalith.FrontComposer.sln"))) {
            current = current.Parent;
        }

        current.ShouldNotBeNull("Could not locate repository root.");
        return current;
    }

    private static IEnumerable<string> SourceFiles()
        => Directory.EnumerateFiles(Path.Combine(ProjectRoot().FullName, "src"), "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.Ordinal)
                || path.EndsWith(".razor", StringComparison.Ordinal)
                || path.EndsWith(".csproj", StringComparison.Ordinal)
                || path.EndsWith(".md", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    private static string ProjectRelativePath(string path)
        => Path.GetRelativePath(ProjectRoot().FullName, path).Replace('\\', '/');

    private static string EscapeYamlScalar(string value)
        => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    private static IEnumerable<string> TypeIdConstants(Type type) {
        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static)) {
            if (field.IsLiteral && field.FieldType == typeof(string)
                && field.GetRawConstantValue() is string value
                && value.StartsWith("HFC", StringComparison.Ordinal)) {
                yield return value;
            }
        }
    }

    private static IEnumerable<DiagnosticDescriptor> DiagnosticDescriptorFields() {
        foreach (FieldInfo field in typeof(DiagnosticDescriptors).GetFields(BindingFlags.Public | BindingFlags.Static)) {
            if (field.FieldType == typeof(DiagnosticDescriptor)
                && field.GetValue(null) is DiagnosticDescriptor descriptor) {
                yield return descriptor;
            }
        }
    }

    private static IEnumerable<string> ReleaseRows() {
        string[] releaseFiles = [
            Path.Combine(ProjectRoot().FullName, "src", "Hexalith.FrontComposer.SourceTools", "AnalyzerReleases.Unshipped.md"),
            Path.Combine(ProjectRoot().FullName, "src", "Hexalith.FrontComposer.SourceTools", "AnalyzerReleases.Shipped.md"),
            Path.Combine(ProjectRoot().FullName, "src", "Hexalith.FrontComposer.Shell", "AnalyzerReleases.Unshipped.md"),
        ];

        foreach (string file in releaseFiles.Where(File.Exists)) {
            foreach (Match match in HfcIdRegex().Matches(File.ReadAllText(file))) {
                yield return match.Value;
            }
        }
    }

    [GeneratedRegex("HFC[0-9]{4}", RegexOptions.CultureInvariant)]
    private static partial Regex HfcIdRegex();

    [GeneratedRegex("\\[property:\\s*(?:System\\.)?Obsolete\\(\"(?<message>[^\"]+)\"", RegexOptions.CultureInvariant)]
    private static partial Regex ObsoleteAttributeRegex();

    private sealed record DiagnosticRegistry(
        string SchemaVersion,
        string CanonicalHelpLinkFormat,
        DiagnosticRange[] Ranges,
        string[] ExternalBoundaries,
        DiagnosticEntry[] Diagnostics);

    private sealed record DiagnosticRange(
        string OwnerPackage,
        int Start,
        int End);

    private sealed record DiagnosticEntry(
        string Id,
        string OwnerPackage,
        string Range,
        string Title,
        string Lifecycle,
        string IntroducedIn,
        string? DeprecatedIn,
        string? RemovedIn,
        string? CompilerSeverity,
        string? RuntimeLogLevel,
        string? PanelSeverity,
        string? CliExitBehavior,
        string? McpCategory,
        string MessageTemplate,
        string DocsSlug,
        string HelpLinkUri,
        string RedactionClass,
        string SuppressionPolicy,
        string ReleaseRow,
        string? MigrationId,
        string[] RelatedIds,
        string OwnerStory);
}
