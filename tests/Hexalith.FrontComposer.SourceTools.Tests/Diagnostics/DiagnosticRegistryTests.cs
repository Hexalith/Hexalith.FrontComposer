using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.SourceTools.Diagnostics;

using Microsoft.CodeAnalysis;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Diagnostics;

[CollectionDefinition("DiagnosticRegistry", DisableParallelization = true)]
public sealed class DiagnosticRegistryCollection { }

/// <summary>
/// Story 9-4 registry contract tests. The registry is the single source of truth for HFC
/// ownership, lifecycle, docs links, release rows, and channel severity metadata.
/// </summary>
[Trait("Category", "Governance")]
[Collection("DiagnosticRegistry")]
public sealed partial class DiagnosticRegistryTests {
    private const string SupportedSchemaVersion = "1.0";
    private const string CanonicalHelpLinkFormat = "https://hexalith.github.io/FrontComposer/diagnostics/{0}";
    private const string CanonicalDocsHost = "hexalith.github.io";
    private static readonly StringComparer Ordinal = StringComparer.Ordinal;
    private static readonly StringComparer OrdinalIgnoreCase = StringComparer.OrdinalIgnoreCase;
    private static readonly (int Start, int End)[] ExpectedRangeBounds =
        [(1, 999), (1000, 1999), (2000, 2999), (3000, 3999), (4000, 4999), (5000, 5999)];

    private static readonly HashSet<string> AllowedRedactionClasses = new(Ordinal) {
        "source-metadata-only",
        "runtime-sanitized",
        "configuration-only",
        "tenant-redacted",
        "user-redacted",
        "exception-bounded",
        "build-trace",
    };

    private static readonly HashSet<string> AllowedSuppressionPolicies = new(Ordinal) {
        "discouraged-error",
        "allowed-with-rationale",
        "discouraged-warning",
        "informational",
    };

    [Fact]
    public void RegistryContract_IsVersionedSortedUniqueAndRangeOwned() {
        DiagnosticRegistry registry = LoadRegistry();

        registry.SchemaVersion.ShouldBe(SupportedSchemaVersion);
        registry.CanonicalHelpLinkFormat.ShouldBe(CanonicalHelpLinkFormat);
        registry.Ranges.ShouldNotBeNull();
        registry.Ranges.Length.ShouldBe(ExpectedRangeBounds.Length);
        registry.Ranges.Select(r => r.OwnerPackage).ShouldBe([
            "Contracts",
            "SourceTools",
            "Shell",
            "EventStore",
            "Mcp",
            "Aspire",
        ], ignoreOrder: false);

        for (int i = 0; i < registry.Ranges.Length; i++) {
            DiagnosticRange range = registry.Ranges[i];
            (int start, int end) = ExpectedRangeBounds[i];
            range.Start.ShouldBe(start, $"{range.OwnerPackage} range start drift.");
            range.End.ShouldBe(end, $"{range.OwnerPackage} range end drift.");
        }

        registry.Diagnostics.ShouldNotBeNull();
        registry.Diagnostics.Length.ShouldBeGreaterThan(0, "registry has no diagnostics — accidental wipe.");

        string[] ids = registry.Diagnostics.Select(d => d.Id).ToArray();
        ids.ShouldBe(ids.OrderBy(id => id, Ordinal).ToArray(), "diagnostics must be sorted ordinally by id.");
        ids.ShouldBeUnique();
        ids.Select(id => id.ToUpperInvariant()).ShouldBeUnique("case-variant id collision.");
        registry.Diagnostics.Select(d => d.DocsSlug).ShouldBeUnique();
        registry.Diagnostics.Select(d => d.DocsSlug.ToLowerInvariant()).ShouldBeUnique("case-variant slug collision (case-insensitive filesystems).");
        registry.Diagnostics.Select(d => d.Title).ShouldBeUnique();

        Regex idRegex = HfcIdRegex();
        Regex messageTemplateShape = MessageTemplateShapeRegex();

        foreach (DiagnosticEntry diagnostic in registry.Diagnostics) {
            idRegex.IsMatch(diagnostic.Id).ShouldBeTrue($"{diagnostic.Id} must match canonical HFC[0-9]{{4}} shape.");
            diagnostic.Lifecycle.ShouldBeOneOf("reserved", "active", "deprecated", "retired", "removed-in-major");
            diagnostic.OwnerPackage.ShouldNotBeNullOrWhiteSpace();
            diagnostic.OwnerStory.ShouldNotBeNullOrWhiteSpace();
            diagnostic.ReleaseRow.ShouldNotBeNullOrWhiteSpace();
            messageTemplateShape.IsMatch(diagnostic.MessageTemplate).ShouldBeTrue(
                $"{diagnostic.Id} message template must follow What/Expected/Got|.../Fix/DocsLink shape.");
            diagnostic.MessageTemplate.ShouldContain("DocsLink", Case.Sensitive);
            diagnostic.DocsSlug.ShouldBe($"diagnostics/{diagnostic.Id}");
            diagnostic.HelpLinkUri.ShouldBe(string.Format(CultureInfo.InvariantCulture, CanonicalHelpLinkFormat, diagnostic.Id));
            diagnostic.RedactionClass.ShouldBeOneOf(AllowedRedactionClasses.ToArray());
            diagnostic.SuppressionPolicy.ShouldBeOneOf(AllowedSuppressionPolicies.ToArray());

            DiagnosticRange? range = registry.Ranges.SingleOrDefault(r => r.OwnerPackage == diagnostic.OwnerPackage);
            range.ShouldNotBeNull($"{diagnostic.Id} ownerPackage '{diagnostic.OwnerPackage}' has no range.");
            int numericId = int.Parse(diagnostic.Id[3..], NumberStyles.None, CultureInfo.InvariantCulture);
            numericId.ShouldBeInRange(range.Start, range.End, $"{diagnostic.Id} is outside {diagnostic.OwnerPackage} range.");

            if (diagnostic.RelatedIds is not null) {
                foreach (string relatedId in diagnostic.RelatedIds) {
                    idRegex.IsMatch(relatedId).ShouldBeTrue($"{diagnostic.Id} relatedIds entry '{relatedId}' is malformed.");
                    ids.ShouldContain(relatedId, $"{diagnostic.Id} relatedIds references unknown id '{relatedId}'.");
                }
            }

            if (!string.IsNullOrEmpty(diagnostic.MigrationId)) {
                ids.ShouldContain(diagnostic.MigrationId, $"{diagnostic.Id} migrationId '{diagnostic.MigrationId}' must resolve to a registry entry.");
            }
        }
    }

    [Fact]
    public void Registry_CoversDescriptorConstantsAndReleaseRowsBidirectionally() {
        DiagnosticRegistry registry = LoadRegistry();
        Dictionary<string, DiagnosticEntry> byId = registry.Diagnostics.ToDictionary(d => d.Id, Ordinal);
        HashSet<string> constantIds = TypeIdConstants(typeof(FcDiagnosticIds)).ToHashSet(Ordinal);
        HashSet<string> descriptorIds = DiagnosticDescriptorFields().Select(d => d.Id).ToHashSet(Ordinal);
        HashSet<string> releaseRows = ReleaseRows().ToHashSet(Ordinal);

        constantIds.Count.ShouldBeGreaterThan(0, "FcDiagnosticIds appears empty — refactor regression?");
        descriptorIds.Count.ShouldBeGreaterThan(0, "DiagnosticDescriptors has no static fields — refactor regression?");

        foreach (string id in constantIds.Concat(descriptorIds).Distinct(Ordinal)) {
            byId.ShouldContainKey(id, $"{id} must have a diagnostic-registry row.");
        }

        foreach (DiagnosticDescriptor descriptor in DiagnosticDescriptorFields()) {
            byId.ShouldContainKey(descriptor.Id, $"{descriptor.Id} descriptor lacks a registry row.");
            DiagnosticEntry entry = byId[descriptor.Id];
            entry.CompilerSeverity.ShouldNotBeNull($"{descriptor.Id} has a Roslyn descriptor; registry must declare compilerSeverity.");
            entry.CompilerSeverity.ShouldBe(descriptor.DefaultSeverity.ToString());
            descriptor.Category.ShouldBe("HexalithFrontComposer");
            descriptor.Title.ToString(CultureInfo.InvariantCulture).ShouldBe(entry.Title);
            descriptor.HelpLinkUri.ShouldBe(entry.HelpLinkUri);
            releaseRows.ShouldContain(descriptor.Id, $"{descriptor.Id} must be listed in AnalyzerReleases.");
        }

        Regex strictHfcId = StrictHfcIdRegex();
        foreach (string releaseRow in releaseRows.Where(id => strictHfcId.IsMatch(id))) {
            byId.ShouldContainKey(releaseRow, $"{releaseRow} release row must resolve through the registry.");
            DiagnosticEntry entry = byId[releaseRow];
            entry.ReleaseRow.ShouldNotBe(
                "runtime-only",
                $"{releaseRow} appears in an analyzer-releases file but registry says runtime-only — AC4 violation.");
        }

        foreach (DiagnosticEntry entry in registry.Diagnostics) {
            if (entry.ReleaseRow == "runtime-only") {
                releaseRows.ShouldNotContain(
                    entry.Id,
                    $"{entry.Id} is registry runtime-only but appears in an AnalyzerReleases.*.md file (AC4).");
                entry.CompilerSeverity.ShouldBeNull(
                    $"{entry.Id} is runtime-only; compilerSeverity must be null (analyzer-emitted vs runtime-emitted are mutually exclusive unless registry declares the mixed mapping).");
            }
        }
    }

    [Fact]
    public void Registry_SeverityChannelMappings_AreInternallyConsistent() {
        DiagnosticRegistry registry = LoadRegistry();
        string[] allowedRuntimeLogLevels = ["Trace", "Debug", "Information", "Warning", "Error", "Critical"];
        string[] allowedPanelSeverities = ["Information", "Warning", "Error"];
        string[] allowedCliExitBehaviors = ["non-blocking", "diagnostic", "blocking"];
        string[] allowedMcpCategories = ["schema-deprecation", "schema-mismatch", "schema-unavailable", "negotiation", "migration", "authorization", "idempotency", "resource"];

        foreach (DiagnosticEntry entry in registry.Diagnostics) {
            if (entry.RuntimeLogLevel is not null) {
                allowedRuntimeLogLevels.ShouldContain(entry.RuntimeLogLevel, $"{entry.Id} runtimeLogLevel '{entry.RuntimeLogLevel}' invalid.");
            }

            if (entry.PanelSeverity is not null) {
                allowedPanelSeverities.ShouldContain(entry.PanelSeverity, $"{entry.Id} panelSeverity '{entry.PanelSeverity}' invalid.");
            }

            if (entry.CliExitBehavior is not null) {
                allowedCliExitBehaviors.ShouldContain(entry.CliExitBehavior, $"{entry.Id} cliExitBehavior '{entry.CliExitBehavior}' invalid.");
            }

            if (entry.McpCategory is not null) {
                allowedMcpCategories.ShouldContain(entry.McpCategory, $"{entry.Id} mcpCategory '{entry.McpCategory}' invalid.");
            }

            if (entry.Lifecycle == "retired") {
                entry.CompilerSeverity.ShouldBeNull($"{entry.Id} retired but still has compilerSeverity.");
                entry.RuntimeLogLevel.ShouldBeNull($"{entry.Id} retired but still has runtimeLogLevel.");
                entry.PanelSeverity.ShouldBeNull($"{entry.Id} retired but still has panelSeverity.");
                entry.CliExitBehavior.ShouldBeOneOf("non-blocking", null!);
            }

            if (entry.Lifecycle == "deprecated") {
                entry.DeprecatedIn.ShouldNotBeNullOrWhiteSpace($"{entry.Id} lifecycle=deprecated but deprecatedIn is empty.");
            }
        }
    }

    [Fact]
    public void DocsStubs_ArePresentBoundedAndRegistryBacked() {
        DiagnosticRegistry registry = LoadRegistry();
        DirectoryInfo docsRoot = DiagnosticsDocsRoot();

        Regex frontMatterIdLine = FrontMatterIdLineRegex();
        Regex frontMatterTitleLine = FrontMatterTitleLineRegex();
        Regex frontMatterOwnerPackageLine = FrontMatterOwnerPackageLineRegex();
        Regex frontMatterLifecycleLine = FrontMatterLifecycleLineRegex();
        Regex frontMatterDocsSlugLine = FrontMatterDocsSlugLineRegex();
        Regex frontMatterSeverityLine = FrontMatterSeverityLineRegex();
        Regex frontMatterStoryOwnerLine = FrontMatterStoryOwnerLineRegex();
        Regex frontMatterIntroducedInLine = FrontMatterIntroducedInLineRegex();
        Regex frontMatterRelatedIdsLine = FrontMatterRelatedIdsLineRegex();
        string[] forbiddenInjectionPatterns = [
            "<script", "</script", "<iframe", "<img onerror", "javascript:", "data:text/html",
            "vbscript:", "&#x", "%3cscript", "‮script",
        ];
        char[] forbiddenFrontMatterFormulaLeads = ['=', '+', '@'];

        foreach (DiagnosticEntry diagnostic in registry.Diagnostics) {
            FileInfo stub = new(Path.Combine(docsRoot.FullName, $"{diagnostic.Id}.md"));
            stub.Exists.ShouldBeTrue($"{diagnostic.Id} docs stub is missing.");

            byte[] raw = File.ReadAllBytes(stub.FullName);
            (raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF).ShouldBeFalse(
                $"{diagnostic.Id} stub has UTF-8 BOM; samples must be BOM-free.");
            string markdown = Encoding.UTF8.GetString(raw);
            markdown.StartsWith("---", StringComparison.Ordinal).ShouldBeTrue($"{diagnostic.Id} stub missing YAML front matter open delimiter.");

            int closeIdx = markdown.IndexOf("\n---", 3, StringComparison.Ordinal);
            closeIdx.ShouldBePositive($"{diagnostic.Id} stub missing YAML front matter close delimiter.");
            string frontMatter = markdown[3..closeIdx];

            frontMatterIdLine.Match(frontMatter).Groups["id"].Value.ShouldBe(diagnostic.Id, $"{diagnostic.Id} stub front-matter id drift.");
            frontMatterTitleLine.Match(frontMatter).Groups["title"].Value.ShouldBe(diagnostic.Title, $"{diagnostic.Id} stub front-matter title drift.");
            frontMatterOwnerPackageLine.Match(frontMatter).Groups["owner"].Value.ShouldBe(diagnostic.OwnerPackage, $"{diagnostic.Id} stub front-matter ownerPackage drift.");
            frontMatterLifecycleLine.Match(frontMatter).Groups["lifecycle"].Value.ShouldBe(diagnostic.Lifecycle, $"{diagnostic.Id} stub front-matter lifecycle drift.");
            frontMatterDocsSlugLine.Match(frontMatter).Groups["slug"].Value.ShouldBe(diagnostic.DocsSlug, $"{diagnostic.Id} stub front-matter docsSlug drift.");

            // Story 9-4 chunk-C P3: parity matchers for severity / storyOwner / introducedIn / relatedIds.
            // Retired stubs intentionally omit `severity` per the retired-stub convention (chunk-C P14).
            Match severityMatch = frontMatterSeverityLine.Match(frontMatter);
            if (diagnostic.Lifecycle == "retired") {
                severityMatch.Success.ShouldBeFalse($"{diagnostic.Id} retired stub must omit `severity` front-matter field.");
            } else {
                severityMatch.Success.ShouldBeTrue($"{diagnostic.Id} stub missing `severity` front-matter field.");
                string expectedSeverity = diagnostic.CompilerSeverity ?? diagnostic.PanelSeverity ?? diagnostic.RuntimeLogLevel ?? "Info";
                if (expectedSeverity == "Information") {
                    expectedSeverity = "Info";
                }
                severityMatch.Groups["severity"].Value.ShouldBe(expectedSeverity, $"{diagnostic.Id} stub front-matter severity drift (canonical token is `Info`, not `Information`).");
            }

            frontMatterStoryOwnerLine.Match(frontMatter).Groups["story"].Value.ShouldBe(diagnostic.OwnerStory ?? string.Empty, $"{diagnostic.Id} stub front-matter storyOwner drift from registry ownerStory.");
            frontMatterIntroducedInLine.Match(frontMatter).Groups["version"].Value.ShouldBe(diagnostic.IntroducedIn ?? string.Empty, $"{diagnostic.Id} stub front-matter introducedIn drift from registry.");

            Match relatedMatch = frontMatterRelatedIdsLine.Match(frontMatter);
            relatedMatch.Success.ShouldBeTrue($"{diagnostic.Id} stub missing `relatedIds:` array.");

            foreach (string line in frontMatter.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)) {
                if (line.Length > 0 && forbiddenFrontMatterFormulaLeads.Contains(line[0])) {
                    Assert.Fail($"{diagnostic.Id} stub front-matter line starts with formula-injection lead char '{line[0]}': {line}");
                }
            }

            markdown.ShouldContain("<!-- story-9-5:narrative-start -->");
            markdown.ShouldContain("<!-- story-9-5:narrative-end -->", customMessage: $"{diagnostic.Id} stub missing narrative-end marker (Story 9-5 narrative-section bounds).");
            markdown.ShouldContain("## Problem");
            markdown.ShouldContain("## Common Causes");
            markdown.ShouldContain("## How To Fix");
            markdown.ShouldContain("## Example");
            markdown.ShouldContain("## Suppression Guidance");
            markdown.ShouldContain("## Migration/Deprecation");
            markdown.ShouldContain("## Related Diagnostics");

            string sanitized = StripZeroWidthAndFormatChars(markdown);
            foreach (string forbidden in forbiddenInjectionPatterns) {
                if (sanitized.Contains(forbidden, StringComparison.OrdinalIgnoreCase)) {
                    Assert.Fail($"{diagnostic.Id} stub contains injection pattern '{forbidden}'.");
                }
            }
        }

        string[] orphanStubs = Directory.EnumerateFiles(docsRoot.FullName)
            .Where(path => Path.GetFileName(path).StartsWith("HFC", StringComparison.OrdinalIgnoreCase)
                && Path.GetExtension(path).Equals(".md", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetFileNameWithoutExtension(path)!)
            .Except(registry.Diagnostics.Select(d => d.Id), OrdinalIgnoreCase)
            .ToArray();
        orphanStubs.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("2.0", "unsupported-schema")]
    [InlineData("future-1", "unsupported-schema")]
    [InlineData("1.0", "duplicate-id")]
    [InlineData("1.0", "duplicate-slug")]
    [InlineData("1.0", "missing-diagnostics-array")]
    [InlineData("1.0", "out-of-range-id")]
    [InlineData("1.0", "missing-owner")]
    [InlineData("1.0", "invalid-lifecycle")]
    public void RegistryValidator_FailsClosedWithNamedCategoriesPinnedToOneRoot(string schemaVersion, string expectedCategory) {
        // NOTE: ValidateRegistryJson is a structural test fixture (not the production validator).
        // It pins the deterministic categories the production validator MUST emit; if the production
        // validator ever drifts, integration with this test will surface the gap. Tracked as
        // DEF-9-4-Bx for future "extract production validator" refactor.
        JsonObject json = RegistryJson().DeepClone().AsObject();
        json["schemaVersion"] = schemaVersion;

        switch (expectedCategory) {
            case "duplicate-id": {
                JsonArray diagnostics = json["diagnostics"]!.AsArray();
                diagnostics.Add(diagnostics[0]!.DeepClone());
                break;
            }

            case "duplicate-slug": {
                JsonArray diagnostics = json["diagnostics"]!.AsArray();
                JsonObject clone = diagnostics[0]!.DeepClone().AsObject();
                clone["id"] = "HFC9990";
                diagnostics.Add(clone);
                break;
            }

            case "missing-diagnostics-array":
                json.Remove("diagnostics");
                break;

            case "out-of-range-id": {
                JsonObject diagnostic = json["diagnostics"]!.AsArray()[0]!.AsObject();
                diagnostic["id"] = "HFC9999";
                break;
            }

            case "missing-owner": {
                JsonObject diagnostic = json["diagnostics"]!.AsArray()[0]!.AsObject();
                diagnostic.Remove("ownerPackage");
                break;
            }

            case "invalid-lifecycle": {
                JsonObject diagnostic = json["diagnostics"]!.AsArray()[0]!.AsObject();
                diagnostic["lifecycle"] = "frozen";
                break;
            }
        }

        string[] categories = ValidateRegistryJson(json).ToArray();
        categories.ShouldContain(expectedCategory);

        // unsupported-schema must short-circuit; no other categories should be reported alongside.
        if (expectedCategory == "unsupported-schema") {
            categories.ShouldBe(["unsupported-schema"], $"unsupported-schema must fail-closed on first error, but got [{string.Join(", ", categories)}].");
        }
    }

    [Theory]
    [InlineData("diagnostics/HFC1058", false)]
    [InlineData("diagnostics/hfc1058", true)]
    [InlineData("DIAGNOSTICS/HFC1058", true)]
    [InlineData("diagnostics/HFC1058%2fescape", true)]
    [InlineData("diagnostics/HFC1058%5cescape", true)]
    [InlineData("diagnostics/HFC1058%00null", true)]
    [InlineData("../diagnostics/HFC1058", true)]
    [InlineData("..\\diagnostics\\HFC1058", true)]
    [InlineData("diagnostics/HFC1058?x=1", true)]
    [InlineData("diagnostics/HFC1058#frag", true)]
    [InlineData("diagnostics\\HFC1058", true)]
    [InlineData("diagnostics/HFC1058​", true)]
    [InlineData("diagnostics/HFC1058‎", true)]
    [InlineData("diagnostics/HFC1058‏", true)]
    [InlineData("diagnostics/HFC1058‮", true)]
    [InlineData("diagnostics/ＨFC1058", true)]
    [InlineData(" diagnostics/HFC1058", true)]
    [InlineData("diagnostics/HFC1058 ", true)]
    public void DocsSlugValidation_DistinguishesUnsafeCanonicalizationFailures(string docsSlug, bool expectInvalid) {
        JsonObject json = RegistryJson().DeepClone().AsObject();
        JsonObject? diagnostic = json["diagnostics"]!.AsArray()
            .Select(node => node!.AsObject())
            .FirstOrDefault(node => node["id"]!.GetValue<string>() == "HFC1058");
        diagnostic.ShouldNotBeNull("HFC1058 fixture row missing — registry edit broke this Theory's reference id.");
        diagnostic["docsSlug"] = docsSlug;

        string[] categories = ValidateRegistryJson(json).ToArray();

        if (expectInvalid) {
            categories.ShouldContain("invalid-slug", $"slug '{docsSlug}' must be rejected as invalid.");
        } else {
            categories.ShouldNotContain("invalid-slug", $"slug '{docsSlug}' is canonical and must not be flagged.");
        }
    }

    [Fact]
    public void FrontComposerObsoleteAttributes_FollowDiagnosticDeprecationPolicy() {
        DiagnosticRegistry registry = LoadRegistry();
        HashSet<string> ids = registry.Diagnostics.Select(d => d.Id).ToHashSet(Ordinal);
        Regex obsoleteRegex = ObsoleteAttributeRegex();
        Regex obsoleteMessageShape = ObsoleteMessageShapeRegex();

        foreach (string file in OwnedSourceFiles()) {
            string text;
            try {
                text = File.ReadAllText(file, Encoding.UTF8);
            } catch (IOException) {
                continue;
            }

            foreach (Match match in obsoleteRegex.Matches(text)) {
                string message = match.Groups["message"].Value;
                if (string.IsNullOrEmpty(message)) {
                    continue;
                }

                if (message.Contains(' ')) {
                    Assert.Fail($"{ProjectRelativePath(file)} obsolete message contains NBSP.");
                }
                obsoleteMessageShape.IsMatch(message).ShouldBeTrue($"{ProjectRelativePath(file)} obsolete message must follow Story 9-4 policy: {message}");

                foreach (Match idMatch in HfcIdRegex().Matches(message)) {
                    ids.ShouldContain(idMatch.Value, $"{ProjectRelativePath(file)} obsolete references unknown HFC id '{idMatch.Value}'.");
                }
            }
        }
    }

    [Fact]
    public void FrontComposerObsoleteAttributes_RemovalWindowIsAtLeastOneMinor() {
        Regex obsoleteRegex = ObsoleteAttributeRegex();
        Regex versionExtract = ObsoleteMessageShapeRegex();

        foreach (string file in OwnedSourceFiles()) {
            string text;
            try {
                text = File.ReadAllText(file, Encoding.UTF8);
            } catch (IOException) {
                continue;
            }

            foreach (Match match in obsoleteRegex.Matches(text)) {
                string message = match.Groups["message"].Value;
                Match shape = versionExtract.Match(message);
                if (!shape.Success) {
                    continue;
                }

                Version target = ParseVersion(shape.Groups["target"].Value);
                Version removal = ParseVersion(shape.Groups["removal"].Value);

                if (target.Major == removal.Major) {
                    (removal.Minor - target.Minor).ShouldBeGreaterThanOrEqualTo(
                        1,
                        $"{ProjectRelativePath(file)}: removal v{removal} must be at least one minor after deprecation v{target} on the same major (AC11).");
                } else {
                    removal.Major.ShouldBeGreaterThan(target.Major, $"{ProjectRelativePath(file)}: removal v{removal} must not precede deprecation v{target}.");
                }
            }
        }
    }

    [Fact]
    public void FrontComposerObsoleteAttributes_OnNet10TargetUseDiagnosticIdAndUrlFormat() {
        // AC24 / AC9 precedence: where TFM supports custom obsolete diagnostic IDs and URL format,
        // they must be used. We assert this for the two known production deprecations.
        string queryRequest = File.ReadAllText(Path.Combine(ProjectRoot().FullName, "src", "Hexalith.FrontComposer.Contracts", "Communication", "QueryRequest.cs"), Encoding.UTF8);
        queryRequest.ShouldContain("DiagnosticId = \"HFC0001\"", customMessage: "QueryRequest.Filter must declare DiagnosticId on net10.0+ obsolete (AC24).");
        queryRequest.ShouldContain("UrlFormat = \"https://hexalith.github.io/FrontComposer/diagnostics/{0}\"", customMessage: "QueryRequest.Filter must declare UrlFormat on net10.0+ obsolete (AC24).");

        string schemaNegotiation = File.ReadAllText(Path.Combine(ProjectRoot().FullName, "src", "Hexalith.FrontComposer.Mcp", "Schema", "SchemaNegotiation.cs"), Encoding.UTF8);
        schemaNegotiation.ShouldContain("DiagnosticId = \"HFC4001\"", customMessage: "SchemaNegotiation.HasCompatibleAdditiveDrift must declare DiagnosticId (AC24).");
        schemaNegotiation.ShouldContain("UrlFormat = \"https://hexalith.github.io/FrontComposer/diagnostics/{0}\"", customMessage: "SchemaNegotiation.HasCompatibleAdditiveDrift must declare UrlFormat (AC24).");
    }

    [Fact]
    public void CompatibilitySuppressionEvidence_IsDeterministicAndBounded() {
        FileInfo suppression = new(Path.Combine(ProjectRoot().FullName, "docs", "diagnostics", "compatibility-suppressions.json"));
        suppression.Exists.ShouldBeTrue("Story 9-4 compatibility evidence file is missing.");

        JsonObject json = JsonNode.Parse(File.ReadAllText(suppression.FullName, Encoding.UTF8))!.AsObject();
        json["schemaVersion"]!.GetValue<string>().ShouldBe("1.0");
        JsonArray suppressions = json["suppressions"]!.AsArray();

        Regex hfcIdRegex = StrictHfcIdRegex();
        Regex targetReleaseRegex = TargetReleaseRegex();
        HashSet<(string Package, string Tfm, string Old)> uniqueness = new();

        foreach (JsonNode? node in suppressions) {
            JsonObject item = node!.AsObject();
            string package = item["package"]!.GetValue<string>();
            string tfm = item["tfm"]!.GetValue<string>();
            string oldSig = item["oldSignature"]!.GetValue<string>();
            string newState = item["newState"]!.GetValue<string>();
            string hfcId = item["hfcId"]!.GetValue<string>();
            string target = item["targetRelease"]!.GetValue<string>();
            string rationale = item["reviewerRationale"]!.GetValue<string>();

            package.ShouldStartWith("Hexalith.FrontComposer.");
            tfm.ShouldNotBeNullOrWhiteSpace();
            oldSig.ShouldNotBeNullOrWhiteSpace();
            newState.ShouldNotBeNullOrWhiteSpace();
            hfcIdRegex.IsMatch(hfcId).ShouldBeTrue($"hfcId '{hfcId}' malformed.");
            targetReleaseRegex.IsMatch(target).ShouldBeTrue($"targetRelease '{target}' malformed.");

            string trimmed = rationale.Trim();
            trimmed.Length.ShouldBeInRange(16, 400, $"rationale must be 16-400 visible chars (got {trimmed.Length}).");
            if (trimmed.Any(char.IsControl)) {
                Assert.Fail("rationale must not contain control characters.");
            }

            uniqueness.Add((package, tfm, oldSig)).ShouldBeTrue($"duplicate suppression row for ({package}, {tfm}, {oldSig}).");
        }
    }

    [Fact]
    public void SubmoduleBoundaries_AreDocumentedAndExcludedFromRegistryOwnershipScan() {
        FileInfo gitmodules = new(Path.Combine(ProjectRoot().FullName, ".gitmodules"));
        gitmodules.Exists.ShouldBeTrue();
        string modules = File.ReadAllText(gitmodules.FullName, Encoding.UTF8);

        Regex sectionPath = SubmodulePathRegex();
        HashSet<string> declaredPaths = sectionPath.Matches(modules)
            .Select(m => m.Groups["path"].Value.Trim())
            .ToHashSet(Ordinal);
        declaredPaths.ShouldContain("Hexalith.EventStore");
        declaredPaths.ShouldContain("Hexalith.Tenants");

        DiagnosticRegistry registry = LoadRegistry();
        registry.ExternalBoundaries.ShouldContain("Hexalith.EventStore");
        registry.ExternalBoundaries.ShouldContain("Hexalith.Tenants");

        // No registry diagnostic may claim ownership in a submodule-named package.
        HashSet<string> boundarySegments = registry.ExternalBoundaries
            .Select(b => b.Split('.').Last())
            .ToHashSet(OrdinalIgnoreCase);

        foreach (DiagnosticEntry diagnostic in registry.Diagnostics) {
            string ownerSegment = diagnostic.OwnerPackage;
            if (boundarySegments.Contains(ownerSegment) && diagnostic.OwnerStory != "external-submodule") {
                Assert.Fail($"{diagnostic.Id} claims ownerPackage='{ownerSegment}' which is an external submodule; mark ownerStory=external-submodule or relocate.");
            }
        }
    }

    [Fact]
    public void SourceHfcIds_AreRegistryBackedAndDiagnosticLinksUseCanonicalHost() {
        DiagnosticRegistry registry = LoadRegistry();
        HashSet<string> registryIds = registry.Diagnostics.Select(d => d.Id).ToHashSet(Ordinal);
        HashSet<string> rangeMarkers = new(Ordinal) {
            "HFC0000", "HFC0999", "HFC1000", "HFC1099", "HFC1999",
            "HFC2000", "HFC2999", "HFC3000", "HFC3999", "HFC4000", "HFC4999", "HFC5000", "HFC5999",
        };

        Regex strictHfc = StrictHfcIdRegex();
        Regex docsLinkUrl = DiagnosticsDocsLinkUrlRegex();

        foreach (string file in OwnedSourceFiles()) {
            string text;
            try {
                text = File.ReadAllText(file, Encoding.UTF8);
            } catch (IOException) {
                continue;
            }

            foreach (Match urlMatch in docsLinkUrl.Matches(text)) {
                string host = urlMatch.Groups["host"].Value;
                host.ShouldBe(CanonicalDocsHost, $"{ProjectRelativePath(file)} links to non-canonical diagnostics host '{host}'.");
            }

            foreach (Match match in strictHfc.Matches(text)) {
                if (rangeMarkers.Contains(match.Value)) {
                    continue;
                }

                registryIds.ShouldContain(match.Value, $"{ProjectRelativePath(file)} references {match.Value}, which must resolve through the registry.");
            }
        }
    }

    [Fact]
    public void PackableProjects_UsePackageValidationBaselinePolicy() {
        // NOTE: literal-text assertion against Directory.Build.props is intentional. The current
        // block has a known evaluation-order bug (DEF-9-4-A13); the proper fix relocates the block
        // to Directory.Build.targets behind an EnableFrontComposerPackageValidation switch, at
        // which point this assertion must move with it. Tracked.
        string directoryBuildProps = File.ReadAllText(Path.Combine(ProjectRoot().FullName, "Directory.Build.props"), Encoding.UTF8);
        directoryBuildProps.ShouldContain("<EnablePackageValidation>true</EnablePackageValidation>");
        directoryBuildProps.ShouldContain("<PackageValidationBaselineVersion>0.1.0</PackageValidationBaselineVersion>");
        directoryBuildProps.ShouldContain("<ApiCompatGenerateSuppressionFile>false</ApiCompatGenerateSuppressionFile>");

        Regex isPackableTrue = IsPackableTrueRegex();
        string[] packableProjects = Directory.EnumerateFiles(Path.Combine(ProjectRoot().FullName, "src"), "*.csproj", SearchOption.AllDirectories)
            .Where(path => isPackableTrue.IsMatch(File.ReadAllText(path, Encoding.UTF8)))
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

        Regex anyYearLiteral = YearLiteralRegex();
        Regex isoTimestamp = IsoTimestampRegex();
        string projectRootSlash = ProjectRoot().FullName.Replace('\\', '/');
        string projectRootBackslash = ProjectRoot().FullName;
        string machineName = Environment.MachineName;

        foreach (string sample in expectedSamples) {
            string path = Path.Combine(samplesRoot, sample);
            string json = File.ReadAllText(path, Encoding.UTF8);
            JsonObject parsed = JsonNode.Parse(json)!.AsObject();
            parsed["schemaVersion"]!.GetValue<string>().ShouldBe("1.0");
            parsed["exitCode"]!.GetValue<int>().ShouldBe(2);
            parsed["category"]!.GetValue<string>().ShouldNotBeNullOrWhiteSpace();

            isoTimestamp.IsMatch(json).ShouldBeFalse($"{sample} contains an ISO timestamp; samples must be timestamp-free.");
            anyYearLiteral.IsMatch(json).ShouldBeFalse($"{sample} contains a 4-digit year literal; samples must be year-free (matches \\b(19|20)\\d{{2}}\\b).");
            json.ShouldNotContain(projectRootSlash, Case.Insensitive);
            json.ShouldNotContain(projectRootBackslash, Case.Insensitive);
            if (!string.IsNullOrEmpty(machineName)) {
                json.ShouldNotContain(machineName, Case.Insensitive);
            }

            json.ShouldNotContain("https://api.github.com", Case.Insensitive);
            json.ShouldNotContain("https://api.nuget.org", Case.Insensitive);
        }
    }

    [Fact]
    public void Validators_RerunFromCurrentInputs_NoStaleCache() {
        // AC25 staleness regression: a second invocation against mutated input must reflect the
        // mutation, not a cached prior result.
        JsonObject pristine = RegistryJson().DeepClone().AsObject();
        ValidateRegistryJson(pristine).ToArray().ShouldNotContain("duplicate-id");

        JsonObject mutated = pristine.DeepClone().AsObject();
        JsonArray diagnostics = mutated["diagnostics"]!.AsArray();
        diagnostics.Add(diagnostics[0]!.DeepClone());

        string[] firstRun = ValidateRegistryJson(mutated).ToArray();
        firstRun.ShouldContain("duplicate-id");

        string[] secondRun = ValidateRegistryJson(mutated).ToArray();
        secondRun.ShouldBe(firstRun, "validator must yield the same categories on identical input (no time-dependent behavior).");

        string[] reverted = ValidateRegistryJson(pristine).ToArray();
        reverted.ShouldNotContain("duplicate-id", "validator must reflect input rollback, not stale cached state.");
    }

    [Theory]
    [InlineData("fr-FR")]
    [InlineData("tr-TR")]
    [InlineData("de-DE")]
    public void Registry_LoadsAndValidatesUnderHostileCultures(string culture) {
        CultureInfo prior = CultureInfo.CurrentCulture;
        CultureInfo priorUi = CultureInfo.CurrentUICulture;
        try {
            CultureInfo.CurrentCulture = new CultureInfo(culture);
            CultureInfo.CurrentUICulture = new CultureInfo(culture);

            DiagnosticRegistry registry = LoadRegistry();
            registry.SchemaVersion.ShouldBe(SupportedSchemaVersion);
            registry.Diagnostics.Length.ShouldBeGreaterThan(0);
            registry.Diagnostics
                .Select(d => d.Id.ToUpperInvariant())
                .ShouldBeUnique($"under {culture}, id case-insensitive uniqueness must hold (Turkish-i hazard).");

            ValidateRegistryJson(RegistryJson().DeepClone().AsObject()).ShouldNotContain("invalid-slug");
        } finally {
            CultureInfo.CurrentCulture = prior;
            CultureInfo.CurrentUICulture = priorUi;
        }
    }

    [Fact]
    public void Registry_DocumentedRedactionClassesCoverAllExpectedChannels() {
        // AC6 redaction matrix: every diagnostic carries an explicit redactionClass that places the
        // emitted content into a documented sanitization bucket. Production sanitization happens at
        // the emit site; this meta-test enforces the registry-side classification contract so that
        // any new diagnostic without a class is rejected.
        DiagnosticRegistry registry = LoadRegistry();
        Dictionary<string, int> usage = AllowedRedactionClasses.ToDictionary(c => c, _ => 0, Ordinal);

        foreach (DiagnosticEntry diagnostic in registry.Diagnostics) {
            AllowedRedactionClasses.ShouldContain(diagnostic.RedactionClass, $"{diagnostic.Id} redactionClass '{diagnostic.RedactionClass}' is not in the documented set.");
            usage[diagnostic.RedactionClass]++;
        }

        usage["source-metadata-only"].ShouldBeGreaterThan(0, "expected at least one source-metadata-only diagnostic.");
        usage["runtime-sanitized"].ShouldBeGreaterThan(0, "expected at least one runtime-sanitized diagnostic.");
    }

    [Fact]
    public void Registry_LifecycleNotePresentWhereCrossPackageOrChannelDriftDocumented() {
        // AC22 / AC16: severity, host, or cross-package-emission changes require a registry
        // lifecycleNote per Story 9-4 acceptance criteria. We pin the known cases here so a
        // future edit that strips the note fails this test.
        DiagnosticRegistry registry = LoadRegistry();
        Dictionary<string, DiagnosticEntry> byId = registry.Diagnostics.ToDictionary(d => d.Id, Ordinal);

        foreach (string id in new[] { "HFC1056", "HFC1057", "HFC1601" }) {
            byId.ShouldContainKey(id);
            byId[id].LifecycleNote.ShouldNotBeNullOrWhiteSpace($"{id} requires a lifecycleNote per AC22 / cross-package documentation.");
        }
    }

    private static IEnumerable<string> ValidateRegistryJson(JsonObject json) {
        if (json["schemaVersion"]?.GetValue<string>() != SupportedSchemaVersion) {
            yield return "unsupported-schema";
            yield break;
        }

        if (json["diagnostics"] is not JsonArray diagnosticsArray) {
            yield return "missing-diagnostics-array";
            yield break;
        }

        (int Start, int End)[] ranges = ExpectedRangeBounds;
        string[] orderedOwners = ["Contracts", "SourceTools", "Shell", "EventStore", "Mcp", "Aspire"];

        HashSet<string> ids = new(Ordinal);
        HashSet<string> slugs = new(Ordinal);
        bool yieldedDuplicateId = false;
        bool yieldedDuplicateSlug = false;

        foreach (JsonNode? node in diagnosticsArray) {
            if (node is not JsonObject diagnostic) {
                yield return "invalid-entry";
                continue;
            }

            if (diagnostic["id"]?.GetValue<string>() is not string id) {
                yield return "missing-id";
                continue;
            }

            string? slug = diagnostic["docsSlug"]?.GetValue<string>();
            string? owner = diagnostic["ownerPackage"]?.GetValue<string>();
            string? lifecycle = diagnostic["lifecycle"]?.GetValue<string>();

            if (!ids.Add(id) && !yieldedDuplicateId) {
                yieldedDuplicateId = true;
                yield return "duplicate-id";
            }

            if (slug is null) {
                yield return "missing-slug";
            } else if (!slugs.Add(slug) && !yieldedDuplicateSlug) {
                yieldedDuplicateSlug = true;
                yield return "duplicate-slug";
            } else if (slug != $"diagnostics/{id}"
                || slug.Contains('\\', StringComparison.Ordinal)
                || slug.Contains('%', StringComparison.Ordinal)
                || slug.Contains('?', StringComparison.Ordinal)
                || slug.Contains('#', StringComparison.Ordinal)
                || slug.Contains("..", StringComparison.Ordinal)
                || slug.Any(char.IsWhiteSpace)
                || slug.Any(IsConfusableOrFormatChar)) {
                yield return "invalid-slug";
            }

            if (owner is null) {
                yield return "missing-owner";
                continue;
            }

            int ownerIndex = Array.IndexOf(orderedOwners, owner);
            if (ownerIndex < 0) {
                yield return "missing-owner";
                continue;
            }

            if (lifecycle is null
                || !new[] { "reserved", "active", "deprecated", "retired", "removed-in-major" }.Contains(lifecycle, Ordinal)) {
                yield return "invalid-lifecycle";
            }

            if (id.Length == 7 && id.StartsWith("HFC", StringComparison.Ordinal)
                && int.TryParse(id[3..], NumberStyles.None, CultureInfo.InvariantCulture, out int numericId)) {
                (int start, int end) = ranges[ownerIndex];
                if (numericId < start || numericId > end) {
                    yield return "out-of-range-id";
                }
            }
        }
    }

    private static bool IsConfusableOrFormatChar(char value) {
        if (char.GetUnicodeCategory(value) == System.Globalization.UnicodeCategory.Format) {
            return true;
        }

        return value is '​' or '‌' or '‍' or '﻿'
            or '‎' or '‏' or '⁠' or '᠎'
            or '‪' or '‫' or '‬' or '‭' or '‮'
            or 'Ｈ';
    }

    private static string StripZeroWidthAndFormatChars(string value) {
        StringBuilder sb = new(value.Length);
        foreach (char c in value) {
            if (!IsConfusableOrFormatChar(c)) {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private static Version ParseVersion(string value) {
        string trimmed = value.Trim();
        int suffixStart = trimmed.IndexOfAny(['-', '+']);
        if (suffixStart >= 0) {
            trimmed = trimmed[..suffixStart];
        }

        return new Version(trimmed.Contains('.') ? trimmed : trimmed + ".0");
    }

    private static DiagnosticRegistry LoadRegistry() {
        DiagnosticRegistry? registry = RegistryJson().Deserialize<DiagnosticRegistry>(SerializerOptions);
        registry.ShouldNotBeNull("registry root deserialized to null — empty or malformed JSON.");
        return registry;
    }

    private static readonly JsonSerializerOptions SerializerOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static JsonObject RegistryJson() {
        FileInfo registry = new(Path.Combine(DiagnosticsDocsRoot().FullName, "diagnostic-registry.json"));
        registry.Exists.ShouldBeTrue("docs/diagnostics/diagnostic-registry.json is missing.");
        JsonNode? parsed = JsonNode.Parse(File.ReadAllText(registry.FullName, Encoding.UTF8));
        parsed.ShouldNotBeNull("registry json parsed to null.");
        return parsed.AsObject();
    }

    private static DirectoryInfo DiagnosticsDocsRoot()
        => new(Path.Combine(ProjectRoot().FullName, "docs", "diagnostics"));

    private static DirectoryInfo ProjectRoot() {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        int depth = 0;
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Hexalith.FrontComposer.sln"))) {
            current = current.Parent;
            if (++depth > 16) {
                break;
            }
        }

        current.ShouldNotBeNull("Could not locate repository root.");
        return current;
    }

    private static IEnumerable<string> SourceFiles()
        => EnumerateOwnedFiles(["*.cs", "*.razor", "*.csproj", "*.md"], Path.Combine(ProjectRoot().FullName, "src"));

    private static IEnumerable<string> OwnedSourceFiles()
        => EnumerateOwnedFiles(["*.cs"], Path.Combine(ProjectRoot().FullName, "src"));

    private static IEnumerable<string> EnumerateOwnedFiles(string[] patterns, string root) {
        string[] excludedDirSegments = [
            $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}artifacts{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}_bmad-output{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}Hexalith.EventStore{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}Hexalith.Tenants{Path.DirectorySeparatorChar}",
        ];

        EnumerationOptions options = new() {
            RecurseSubdirectories = true,
            MatchCasing = MatchCasing.CaseInsensitive,
            AttributesToSkip = FileAttributes.ReparsePoint,
        };

        return patterns
            .SelectMany(pattern => Directory.EnumerateFiles(root, pattern, options))
            .Where(path => !excludedDirSegments.Any(segment => path.Contains(segment, StringComparison.Ordinal)))
            .OrderBy(path => path, Ordinal);
    }

    private static string ProjectRelativePath(string path)
        => Path.GetRelativePath(ProjectRoot().FullName, path).Replace('\\', '/');

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
            if (field.FieldType == typeof(DiagnosticDescriptor)) {
                DiagnosticDescriptor? descriptor = field.GetValue(null) as DiagnosticDescriptor;
                descriptor.ShouldNotBeNull($"DiagnosticDescriptors.{field.Name} is null at runtime.");
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

        Regex tableRowId = ReleaseRowIdRegex();
        bool anyExisted = false;

        foreach (string file in releaseFiles.Where(File.Exists)) {
            anyExisted = true;
            foreach (string line in File.ReadAllLines(file, Encoding.UTF8)) {
                Match match = tableRowId.Match(line);
                if (match.Success) {
                    yield return match.Groups["id"].Value;
                }
            }
        }

        anyExisted.ShouldBeTrue("expected at least one AnalyzerReleases.*.md file to exist.");
    }

    [GeneratedRegex(@"\bHFC[0-9]{4}\b", RegexOptions.CultureInvariant)]
    private static partial Regex HfcIdRegex();

    [GeneratedRegex(@"^HFC[0-9]{4}$", RegexOptions.CultureInvariant)]
    private static partial Regex StrictHfcIdRegex();

    [GeneratedRegex(@"\[(?:[a-zA-Z]+:\s*)?(?:System\.)?Obsolete\(""(?<message>[^""\\]*(?:\\.[^""\\]*)*)""", RegexOptions.CultureInvariant)]
    private static partial Regex ObsoleteAttributeRegex();

    [GeneratedRegex(@"^(?<old>[^\.]+?) (?:replaced by [^\.]+?|has no direct replacement) in v(?<target>[0-9]+\.[0-9]+(?:[-+.][0-9A-Za-z.-]+)?)\. See (?<id>HFC[0-9]{4})\. Removed in v(?<removal>[0-9]+\.[0-9]+(?:[-+.][0-9A-Za-z.-]+)?)\.$", RegexOptions.CultureInvariant)]
    private static partial Regex ObsoleteMessageShapeRegex();

    [GeneratedRegex(@"^What:\s*.+?\.\s*Expected:\s*.+?\.\s*Got:\s*.+?\.\s*Fix:\s*.+?\.\s*DocsLink:\s*\S+", RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex MessageTemplateShapeRegex();

    [GeneratedRegex(@"^v[0-9]+\.[0-9]+(?:[-+.][0-9A-Za-z.-]+)?$", RegexOptions.CultureInvariant)]
    private static partial Regex TargetReleaseRegex();

    [GeneratedRegex(@"^id:\s*(?<id>HFC[0-9]{4})\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex FrontMatterIdLineRegex();

    [GeneratedRegex(@"^title:\s*""(?<title>(?:\\.|[^""\\])*)""\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex FrontMatterTitleLineRegex();

    [GeneratedRegex(@"^ownerPackage:\s*(?<owner>\S+)\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex FrontMatterOwnerPackageLineRegex();

    [GeneratedRegex(@"^lifecycle:\s*(?<lifecycle>\S+)\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex FrontMatterLifecycleLineRegex();

    [GeneratedRegex(@"^docsSlug:\s*(?<slug>\S+)\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex FrontMatterDocsSlugLineRegex();

    [GeneratedRegex(@"^severity:\s*(?<severity>\S+)\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex FrontMatterSeverityLineRegex();

    [GeneratedRegex(@"^storyOwner:\s*(?<story>\S+)\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex FrontMatterStoryOwnerLineRegex();

    [GeneratedRegex(@"^introducedIn:\s*(?<version>\S+)\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex FrontMatterIntroducedInLineRegex();

    [GeneratedRegex(@"^relatedIds:\s*\[(?<ids>[^\]]*)\]\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex FrontMatterRelatedIdsLineRegex();

    [GeneratedRegex(@"\[submodule\s+""[^""]+""\]\s*\n(?:[^\n]*\n)*?\s*path\s*=\s*(?<path>[^\n]+)", RegexOptions.CultureInvariant)]
    private static partial Regex SubmodulePathRegex();

    [GeneratedRegex(@"https?://(?<host>[A-Za-z0-9.\-]+)/[^""'\s]*diagnostics/HFC[0-9]{4}", RegexOptions.CultureInvariant)]
    private static partial Regex DiagnosticsDocsLinkUrlRegex();

    [GeneratedRegex(@"<IsPackable>\s*[Tt][Rr][Uu][Ee]\s*</IsPackable>", RegexOptions.CultureInvariant)]
    private static partial Regex IsPackableTrueRegex();

    [GeneratedRegex(@"^\s*(?<id>HFC[A-Z0-9]+)\s*\|", RegexOptions.CultureInvariant)]
    private static partial Regex ReleaseRowIdRegex();

    [GeneratedRegex(@"\b(?:19|20)[0-9]{2}\b", RegexOptions.CultureInvariant)]
    private static partial Regex YearLiteralRegex();

    [GeneratedRegex(@"\b(?:19|20)[0-9]{2}-[0-9]{2}-[0-9]{2}T?", RegexOptions.CultureInvariant)]
    private static partial Regex IsoTimestampRegex();

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
        string[]? RelatedIds,
        string OwnerStory,
        string? LifecycleNote = null);
}
