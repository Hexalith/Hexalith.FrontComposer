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

        registry.ExternalBoundaries.Select(b => b.Package).ShouldBe([
            "Hexalith.EventStore",
            "Hexalith.Tenants",
        ], ignoreOrder: false);
        registry.ExternalBoundaries.Single(b => b.Package == "Hexalith.EventStore").RangePolicy.ShouldBe("owns-range");
        registry.ExternalBoundaries.Single(b => b.Package == "Hexalith.Tenants").RangePolicy.ShouldBe("no-range-reserved");
        registry.AllowedExceptions.CrossPackageRange.Length.ShouldBe(1, "HFC1601 must be the only cross-package range exception.");
        CrossPackageRangeException hfc1601Exception = registry.AllowedExceptions.CrossPackageRange.Single();
        hfc1601Exception.Id.ShouldBe("HFC1601");
        hfc1601Exception.OwnerPackage.ShouldBe("SourceTools");
        hfc1601Exception.ConsumingPackage.ShouldBe("Shell");
        hfc1601Exception.NumericRangeOwner.ShouldBe("SourceTools");
        hfc1601Exception.ApprovingStory.ShouldBe("11-2-diagnostic-registry-and-documentation-governance-follow-ups");
        hfc1601Exception.HelpLinkUri.ShouldBe(string.Format(CultureInfo.InvariantCulture, CanonicalHelpLinkFormat, "HFC1601"));

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
                    // Story 11.2 code-review P12 (cluster 27): self-reference in relatedIds is rejected.
                    relatedId.ShouldNotBe(diagnostic.Id, $"{diagnostic.Id} relatedIds must not include itself.");
                }
            }

            if (!string.IsNullOrEmpty(diagnostic.MigrationId)) {
                ids.ShouldContain(diagnostic.MigrationId, $"{diagnostic.Id} migrationId '{diagnostic.MigrationId}' must resolve to a registry entry.");
            }
        }

        Dictionary<string, DiagnosticEntry> byId = registry.Diagnostics.ToDictionary(d => d.Id, Ordinal);

        // Story 11.2 code-review P29: introducedIn must follow a SemVer-like shape so that
        // "what's new since vX" semantics in Story 9-2 / 9-1 cannot drift to free-form strings.
        Regex semverShape = SemverShapeRegex();
        foreach (DiagnosticEntry diagnostic in registry.Diagnostics) {
            if (string.IsNullOrEmpty(diagnostic.IntroducedIn)) {
                continue;
            }
            semverShape.IsMatch(diagnostic.IntroducedIn).ShouldBeTrue(
                $"{diagnostic.Id} introducedIn '{diagnostic.IntroducedIn}' must follow MAJOR.MINOR or MAJOR.MINOR.PATCH SemVer shape.");
        }

        foreach (DiagnosticEntry diagnostic in registry.Diagnostics.Where(d => d.RelatedIds is { Length: > 0 })) {
            foreach (string relatedId in diagnostic.RelatedIds!) {
                // Story 11.2 code-review P12: TryGetValue replaces byId[relatedId] indexing so a
                // typo yields a clean "unknown-related-id" diagnostic instead of KeyNotFoundException.
                if (!byId.TryGetValue(relatedId, out DiagnosticEntry? relatedEntry)) {
                    Assert.Fail($"{diagnostic.Id} relatedIds references unknown id '{relatedId}'.");
                }
                relatedEntry!.RelatedIds.ShouldNotBeNull($"{relatedId} must reciprocate relatedIds from {diagnostic.Id}.");
                relatedEntry.RelatedIds!.ShouldContain(diagnostic.Id, $"{diagnostic.Id} and {relatedId} relatedIds must be reciprocal.");
            }
        }

        // Story 11.2 code-review P23: migrationId must not point at a retired or removed-in-major
        // diagnostic, otherwise the CLI/migrate path would surface a dead pointer.
        foreach (DiagnosticEntry diagnostic in registry.Diagnostics.Where(d => !string.IsNullOrEmpty(d.MigrationId))) {
            if (!byId.TryGetValue(diagnostic.MigrationId!, out DiagnosticEntry? migrationTarget)) {
                continue;
            }
            migrationTarget.Lifecycle.ShouldNotBeOneOf(
                ["retired", "removed-in-major"],
                $"{diagnostic.Id} migrationId '{diagnostic.MigrationId}' resolves to a {migrationTarget.Lifecycle} diagnostic; migrations must point at active/deprecated targets.");
        }

        byId["HFC1037"].RelatedIds.ShouldBe(["HFC1040", "HFC1044", "HFC1601"], ignoreOrder: false);
        byId["HFC1056"].RelatedIds.ShouldBe(["HFC1057"], ignoreOrder: false);
        byId["HFC1601"].RelatedIds.ShouldBe(["HFC1037", "HFC1040", "HFC1044"], ignoreOrder: false);
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

            // Story 11.2 code-review P2: parse the array contents and assert they match the
            // registry's relatedIds for this diagnostic. A stub may not declare unknown ids
            // (e.g., HFC9999) or drift away from the registry's authoritative set.
            string[] stubRelatedIds = relatedMatch.Groups["ids"].Value
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            string[] registryRelatedIds = diagnostic.RelatedIds ?? [];
            stubRelatedIds.OrderBy(id => id, Ordinal).ShouldBe(
                registryRelatedIds.OrderBy(id => id, Ordinal),
                $"{diagnostic.Id} stub front-matter relatedIds drift from registry: stub=[{string.Join(", ", stubRelatedIds)}], registry=[{string.Join(", ", registryRelatedIds)}].");

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
    [InlineData("missing")]
    [InlineData("malformed")]
    [InlineData("unknown")]
    [InlineData("future")]
    public void RegistryValidator_UnsupportedSchemaShortCircuitsBeforeNestedRows(string schemaCase) {
        JsonObject json = RegistryJson().DeepClone().AsObject();
        json.Remove("diagnostics");

        switch (schemaCase) {
            case "missing":
                json.Remove("schemaVersion");
                break;
            case "malformed":
                json["schemaVersion"] = 10;
                break;
            case "unknown":
                json["schemaVersion"] = "0.9";
                break;
            case "future":
                json["schemaVersion"] = "2.0";
                break;
        }

        ValidateRegistryJson(json).ToArray().ShouldBe(["unsupported-schema"]);
    }

    [Theory]
    [InlineData("diagnostics/HFC1058", false)]
    [InlineData("diagnostics/hfc1058", true)]
    [InlineData("DIAGNOSTICS/HFC1058", true)]
    [InlineData("diagnostics/HFC1058%2fescape", true)]
    [InlineData("diagnostics/HFC1058%5cescape", true)]
    [InlineData("diagnostics/HFC1058%252fescape", true)]
    [InlineData("diagnostics/HFC1058%zz", true)]
    [InlineData("diagnostics/HFC1058%u2215escape", true)]
    [InlineData("diagnostics/HFC1058/../HFC1058", true)]
    [InlineData("/diagnostics/HFC1058", true)]
    [InlineData("C:/diagnostics/HFC1058", true)]
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
        ValidateCompatibilitySuppressionsJson(json).ShouldBeEmpty();
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

    [Theory]
    [InlineData("missing-package", "suppression-missing-package")]
    [InlineData("unknown-diagnostic", "suppression-unknown-diagnostic")]
    [InlineData("wildcard-package", "suppression-wildcard-scope")]
    [InlineData("duplicate-row", "suppression-duplicate-row")]
    [InlineData("expired-row", "suppression-expired")]
    [InlineData("unknown-reason", "suppression-unknown-reason")]
    public void CompatibilitySuppressionValidator_FailsClosedWithNamedCategories(string mutation, string expectedCategory) {
        JsonObject json = JsonNode.Parse("""
            {
              "schemaVersion": "1.0",
              "baselinePolicy": "fixture",
              "suppressions": [
                {
                  "package": "Hexalith.FrontComposer.Contracts",
                  "tfm": "net10.0",
                  "oldSignature": "M:Example.Old",
                  "newState": "removed",
                  "hfcId": "HFC0001",
                  "targetRelease": "v1.0",
                  "reviewerRationale": "Intentional binary break reviewed for this fixture.",
                  "ownerStory": "11-2-diagnostic-registry-and-documentation-governance-follow-ups",
                  "expiresAfter": "v9.9",
                  "reason": "intentional-major-break"
                }
              ]
            }
            """)!.AsObject();
        JsonObject row = json["suppressions"]!.AsArray()[0]!.AsObject();

        switch (mutation) {
            case "missing-package":
                row.Remove("package");
                break;
            case "unknown-diagnostic":
                row["hfcId"] = "HFC9999";
                break;
            case "wildcard-package":
                row["package"] = "*";
                break;
            case "duplicate-row":
                json["suppressions"]!.AsArray().Add(row.DeepClone());
                break;
            case "expired-row":
                row["expiresAfter"] = "v0.1";
                break;
            case "unknown-reason":
                row["reason"] = "anything-goes";
                break;
        }

        ValidateCompatibilitySuppressionsJson(json).ShouldContain(expectedCategory);
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
        registry.ExternalBoundaries.Select(b => b.Package).ShouldContain("Hexalith.EventStore");
        registry.ExternalBoundaries.Select(b => b.Package).ShouldContain("Hexalith.Tenants");

        // No registry diagnostic may claim ownership in a submodule-named package.
        HashSet<string> boundarySegments = registry.ExternalBoundaries
            .Select(b => b.Package.Split('.').Last())
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
                string scheme = urlMatch.Groups["scheme"].Value;
                string suffix = urlMatch.Groups["suffix"].Value;
                host.ShouldBe(CanonicalDocsHost, $"{ProjectRelativePath(file)} links to non-canonical diagnostics host '{host}'.");
                // Story 11.2 code-review P25: scheme must be https and the URL must not carry
                // a trailing slash, query, or fragment after the HFC id.
                scheme.ShouldBe("https", $"{ProjectRelativePath(file)} uses non-https diagnostics scheme '{scheme}'.");
                suffix.ShouldBe(string.Empty, $"{ProjectRelativePath(file)} diagnostics URL has trailing suffix '{suffix}'; canonical form is the bare /HFCxxxx path.");
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
        string directoryBuildProps = File.ReadAllText(Path.Combine(ProjectRoot().FullName, "Directory.Build.props"), Encoding.UTF8);
        directoryBuildProps.ShouldNotContain("<EnablePackageValidation>true</EnablePackageValidation>", customMessage: "package validation depends on IsPackable and must not live in Directory.Build.props.");

        string directoryBuildTargets = File.ReadAllText(Path.Combine(ProjectRoot().FullName, "Directory.Build.targets"), Encoding.UTF8);
        directoryBuildTargets.ShouldContain("<EnableFrontComposerPackageValidation Condition=\"'$(EnableFrontComposerPackageValidation)' == ''\">false</EnableFrontComposerPackageValidation>");
        directoryBuildTargets.ShouldContain("Condition=\"'$(IsPackable)' == 'true' AND '$(EnableFrontComposerPackageValidation)' == 'true'\"");
        directoryBuildTargets.ShouldContain("<EnablePackageValidation>true</EnablePackageValidation>");
        directoryBuildTargets.ShouldContain("<PackageValidationBaselineVersion>$(FrontComposerPackageValidationBaselineVersion)</PackageValidationBaselineVersion>");
        directoryBuildTargets.ShouldContain("<ApiCompatGenerateSuppressionFile>false</ApiCompatGenerateSuppressionFile>");

        Regex isPackableTrue = IsPackableTrueRegex();
        string[] packableProjects = Directory.EnumerateFiles(Path.Combine(ProjectRoot().FullName, "src"), "*.csproj", SearchOption.AllDirectories)
            .Where(path => isPackableTrue.IsMatch(File.ReadAllText(path, Encoding.UTF8)))
            .Select(ProjectRelativePath)
            .OrderBy(path => path, Ordinal)
            .ToArray();

        packableProjects.ShouldBe([
            "src/Hexalith.FrontComposer.Mcp/Hexalith.FrontComposer.Mcp.csproj",
            "src/Hexalith.FrontComposer.Schema/Hexalith.FrontComposer.Schema.csproj",
            "src/Hexalith.FrontComposer.Testing/Hexalith.FrontComposer.Testing.csproj",
        ], ignoreOrder: false);
    }

    [Fact]
    public void HfcmMigrationFindings_AreCliGovernedNotRoslynReleaseRows() {
        string sourceToolsProject = File.ReadAllText(Path.Combine(ProjectRoot().FullName, "src", "Hexalith.FrontComposer.SourceTools", "Hexalith.FrontComposer.SourceTools.csproj"), Encoding.UTF8);
        sourceToolsProject.ShouldNotContain("RS2002", customMessage: "CLI migration findings must not require broad Roslyn release-tracking suppression.");

        ReleaseRows().Where(id => id.StartsWith("HFCM", StringComparison.Ordinal)).ShouldBeEmpty("HFCM rows are CLI migration findings, not Roslyn analyzer release rows.");

        JsonObject json = JsonNode.Parse(File.ReadAllText(Path.Combine(DiagnosticsDocsRoot().FullName, "migration-findings.json"), Encoding.UTF8))!.AsObject();
        json["schemaVersion"]!.GetValue<string>().ShouldBe(SupportedSchemaVersion);
        json["releaseBucket"]!.GetValue<string>().ShouldBe("cli-migration");

        JsonArray findings = json["findings"]!.AsArray();
        string[] ids = findings.Select(node => node!["id"]!.GetValue<string>()).OrderBy(id => id, Ordinal).ToArray();
        ids.ShouldBe(["HFCM0000", "HFCM0001", "HFCM0002", "HFCM0004", "HFCM9001", "HFCM9002"], ignoreOrder: false);
        ids.ShouldBeUnique();

        Regex semverShape = SemverShapeRegex();
        foreach (JsonObject finding in findings.Select(node => node!.AsObject())) {
            finding["id"]!.GetValue<string>().ShouldMatch("^HFCM[0-9]{4}$");
            finding["category"]!.GetValue<string>().ShouldBe("HexalithFrontComposer.Migration");
            finding["severity"]!.GetValue<string>().ShouldBeOneOf("Info", "Warning", "Error");

            // Story 11.2 code-review P3 (T3): migrationDocsSlug must be containment-safe — no
            // traversal, encoded traversal, query, fragment, or absolute path. The validator
            // applies the same canonical-slug rules used for diagnostic docsSlug.
            string slug = finding["migrationDocsSlug"]!.GetValue<string>();
            slug.ShouldStartWith("migrations/");
            slug.ShouldNotContain("..", customMessage: $"HFCM migrationDocsSlug '{slug}' must not contain `..` traversal.");
            slug.ShouldNotContain("?", customMessage: $"HFCM migrationDocsSlug '{slug}' must not include a query suffix.");
            slug.ShouldNotContain("#", customMessage: $"HFCM migrationDocsSlug '{slug}' must not include a fragment suffix.");
            slug.ShouldNotContain("\\", customMessage: $"HFCM migrationDocsSlug '{slug}' must not include backslashes.");
            slug.ShouldNotContain("%", customMessage: $"HFCM migrationDocsSlug '{slug}' must not include percent-encoded characters.");
            IsRootedSlug(slug).ShouldBeFalse($"HFCM migrationDocsSlug '{slug}' must not be a rooted path.");

            finding["releaseNote"]!.GetValue<string>().ShouldNotBeNullOrWhiteSpace();
            string introducedIn = finding["introducedIn"]!.GetValue<string>();
            introducedIn.ShouldNotBeNullOrWhiteSpace();
            semverShape.IsMatch(introducedIn).ShouldBeTrue($"HFCM introducedIn '{introducedIn}' must follow SemVer shape (P29).");

            // Story 11.2 code-review P3 (T3): every CLI migration finding must carry a per-row
            // ownerStory and a release-provenance block so the release-row contract is auditable
            // even though the row is not a Roslyn descriptor.
            finding["ownerStory"]!.GetValue<string>().ShouldNotBeNullOrWhiteSpace(
                "HFCM finding must carry a per-row ownerStory; release-provenance contract (AC15/AC24).");
            JsonObject provenance = finding["releaseProvenance"]!.AsObject();
            provenance["approvingStory"]!.GetValue<string>().ShouldNotBeNullOrWhiteSpace();
            provenance["approvedOn"]!.GetValue<string>().ShouldNotBeNullOrWhiteSpace();
            provenance["rationale"]!.GetValue<string>().ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Theory]
    [InlineData("duplicate-id")]
    [InlineData("wrong-prefix")]
    [InlineData("wrong-bucket")]
    [InlineData("missing-release-note")]
    [InlineData("missing-owner-story")]
    [InlineData("malformed-introduced-in")]
    [InlineData("missing-release-provenance")]
    public void MigrationFindingsValidator_FailsClosedWithNamedCategories(string mutation) {
        // Story 11.2 code-review P3 (T3 subtask 4): the CLI-specific release-row artifact must
        // fail closed on duplicate, wrong-prefix, wrong-bucket, missing-note, and missing-owner
        // mutations. This is the migration-findings analogue of the registry-validator fixture.
        JsonObject json = JsonNode.Parse(File.ReadAllText(Path.Combine(DiagnosticsDocsRoot().FullName, "migration-findings.json"), Encoding.UTF8))!.AsObject();
        JsonArray findings = json["findings"]!.AsArray();
        JsonObject row = findings[0]!.AsObject();

        switch (mutation) {
            case "duplicate-id":
                findings.Add(row.DeepClone());
                break;
            case "wrong-prefix":
                row["id"] = "HFC0001";
                break;
            case "wrong-bucket":
                json["releaseBucket"] = "analyzer-release";
                break;
            case "missing-release-note":
                row.Remove("releaseNote");
                break;
            case "missing-owner-story":
                row.Remove("ownerStory");
                break;
            case "malformed-introduced-in":
                row["introducedIn"] = "latest";
                break;
            case "missing-release-provenance":
                row.Remove("releaseProvenance");
                break;
        }

        string expectedCategory = mutation switch {
            "duplicate-id" => "hfcm-duplicate-id",
            "wrong-prefix" => "hfcm-wrong-prefix",
            "wrong-bucket" => "hfcm-wrong-bucket",
            "missing-release-note" => "hfcm-missing-release-note",
            "missing-owner-story" => "hfcm-missing-owner-story",
            "malformed-introduced-in" => "hfcm-malformed-introduced-in",
            "missing-release-provenance" => "hfcm-missing-release-provenance",
            _ => throw new InvalidOperationException(),
        };
        ValidateMigrationFindingsJson(json).ShouldContain(expectedCategory);
    }

    private static IEnumerable<string> ValidateMigrationFindingsJson(JsonObject json) {
        if (!TryGetString(json, "schemaVersion", out string? schemaVersion) || schemaVersion != SupportedSchemaVersion) {
            yield return "hfcm-unsupported-schema";
            yield break;
        }
        if (!TryGetString(json, "releaseBucket", out string? releaseBucket) || releaseBucket != "cli-migration") {
            yield return "hfcm-wrong-bucket";
        }
        if (json["findings"] is not JsonArray findings) {
            yield return "hfcm-missing-findings-array";
            yield break;
        }

        Regex idShape = HfcmIdShapeRegex();
        Regex semverShape = SemverShapeRegex();
        HashSet<string> seen = new(Ordinal);
        foreach (JsonNode? node in findings) {
            if (node is not JsonObject row) {
                yield return "hfcm-invalid-row";
                continue;
            }
            if (!TryGetString(row, "id", out string? id)) {
                yield return "hfcm-missing-id";
                continue;
            }
            if (!idShape.IsMatch(id!)) {
                yield return "hfcm-wrong-prefix";
                continue;
            }
            if (!seen.Add(id!)) {
                yield return "hfcm-duplicate-id";
            }
            if (!TryGetString(row, "releaseNote", out _)) {
                yield return "hfcm-missing-release-note";
            }
            if (!TryGetString(row, "ownerStory", out _)) {
                yield return "hfcm-missing-owner-story";
            }
            if (!TryGetString(row, "introducedIn", out string? introducedIn) || !semverShape.IsMatch(introducedIn!)) {
                yield return "hfcm-malformed-introduced-in";
            }
            if (row["releaseProvenance"] is not JsonObject provenance
                || !TryGetString(provenance, "approvingStory", out _)
                || !TryGetString(provenance, "approvedOn", out _)
                || !TryGetString(provenance, "rationale", out _)) {
                yield return "hfcm-missing-release-provenance";
            }
        }
    }

    [GeneratedRegex(@"^HFCM[0-9]{4}$", RegexOptions.CultureInvariant)]
    private static partial Regex HfcmIdShapeRegex();

    [Fact]
    public void DriftSampleReports_AreNormalizedAndCommitted() {
        string samplesRoot = Path.Combine(DiagnosticsDocsRoot().FullName, "samples");
        string[] expectedSamples = [
            "compatibility-drift-report.json",
            "docs-stub-drift-report.json",
            "duplicate-id-drift-report.json",
            "encoded-docs-root-escape-report.json",
            "hfcm-release-governance-report.json",
            "invalid-lifecycle-transition-report.json",
            "registry-drift-report.json",
            "release-row-drift-report.json",
            "reserved-retired-misuse-report.json",
            "suppression-scope-drift-report.json",
            "unsafe-generated-front-matter-report.json",
            "unsupported-schema-drift-report.json",
        ];

        Directory.EnumerateFiles(samplesRoot, "*.json")
            .Select(Path.GetFileName)
            .OrderBy(name => name, Ordinal)
            .ShouldBe(expectedSamples, ignoreOrder: false);

        Regex anyYearLiteral = YearLiteralRegex();
        Regex isoTimestamp = IsoTimestampRegex();
        Regex sampleFindingIdShape = SampleFindingIdShapeRegex();
        string projectRootSlash = ProjectRoot().FullName.Replace('\\', '/');
        string projectRootBackslash = ProjectRoot().FullName;
        string machineName = Environment.MachineName;

        // Story 11.2 code-review P16 (AC32): sentinel forbidden-token list. If any drift sample
        // ever contains one of these patterns it must be redacted before commit. Includes Windows
        // and Unix absolute path shapes, $HOME variants, GitHub Actions runner paths, common token
        // prefixes, SDK banners, live feed URLs, and stack-trace markers.
        string[] forbiddenSentinels = [
            "https://api.github.com",
            "https://api.nuget.org",
            "https://pkgs.dev.azure.com",
            "C:\\Users\\",
            "/home/runner/",
            "/Users/",
            "$HOME",
            "${HOME}",
            "~/",
            "ghp_",
            "gho_",
            "xoxb-",
            "AKIA",
            "sk-ant-",
            "Microsoft .NET SDK",
            "   at System.",
            "   at Microsoft.",
            "fv-az",
        ];
        // AC32: max-item / max-character budget. Sample reports are evidence artifacts, not
        // unbounded log dumps; if we ever exceed these the redaction layer is leaking.
        const int MaxFindingsPerSample = 32;
        const int MaxJsonBytesPerSample = 16 * 1024;

        foreach (string sample in expectedSamples) {
            string path = Path.Combine(samplesRoot, sample);
            string json = File.ReadAllText(path, Encoding.UTF8);
            JsonObject parsed = JsonNode.Parse(json)!.AsObject();
            parsed["schemaVersion"]!.GetValue<string>().ShouldBe("1.0");
            parsed["exitCode"]!.GetValue<int>().ShouldBe(2);
            parsed["category"]!.GetValue<string>().ShouldNotBeNullOrWhiteSpace();
            JsonArray findings = parsed["findings"]!.AsArray();
            findings.Count.ShouldBeGreaterThan(0, $"{sample} must include at least one finding.");
            findings.Count.ShouldBeLessThanOrEqualTo(MaxFindingsPerSample, $"{sample} exceeds max findings budget ({findings.Count} > {MaxFindingsPerSample}); redaction/truncation must happen before commit (AC32).");
            json.Length.ShouldBeLessThanOrEqualTo(MaxJsonBytesPerSample, $"{sample} exceeds max sample size ({json.Length} > {MaxJsonBytesPerSample} bytes); samples are evidence artifacts, not log dumps (AC32).");
            foreach (JsonObject finding in findings.Select(node => node!.AsObject())) {
                string findingId = finding["id"]!.GetValue<string>();
                findingId.ShouldNotBeNullOrWhiteSpace();
                // Story 11.2 code-review P24: tighten id pattern to `^HFC[M]?\d{4}$` for real ids
                // or one of the documented placeholders (`HFC-PLACEHOLDER`, `HFCM-PLACEHOLDER`).
                sampleFindingIdShape.IsMatch(findingId).ShouldBeTrue($"{sample} finding id '{findingId}' must be a real HFC[M]?xxxx id or one of the documented placeholders (HFC-PLACEHOLDER, HFCM-PLACEHOLDER).");
                finding["reason"]!.GetValue<string>().ShouldNotBeNullOrWhiteSpace();
                finding["path"]!.GetValue<string>().ShouldNotBeNullOrWhiteSpace();
                finding["message"]!.GetValue<string>().ShouldNotBeNullOrWhiteSpace();
            }

            isoTimestamp.IsMatch(json).ShouldBeFalse($"{sample} contains an ISO timestamp; samples must be timestamp-free.");
            anyYearLiteral.IsMatch(json).ShouldBeFalse($"{sample} contains a 4-digit year literal; samples must be year-free (matches \\b(19|20)\\d{{2}}\\b).");
            json.ShouldNotContain(projectRootSlash, Case.Insensitive);
            json.ShouldNotContain(projectRootBackslash, Case.Insensitive);
            if (!string.IsNullOrEmpty(machineName)) {
                json.ShouldNotContain(machineName, Case.Insensitive);
            }

            foreach (string sentinel in forbiddenSentinels) {
                json.ShouldNotContain(sentinel, Case.Insensitive, $"{sample} contains forbidden sentinel '{sentinel}'; redaction failure (AC32).");
            }
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

    [Fact]
    public void DescriptorCanonicalHelpLinkFormat_EqualsRegistryConstant() {
        // Story 11.2 code-review P17 (AC13): the SourceTools descriptor's canonical-help-link
        // format must equal the registry's canonicalHelpLinkFormat. This is the single-source-of-
        // truth guarantee that DEF-9-4-C4 closure claimed; it must be enforced by a direct test,
        // not just by a hardcoded duplicate constant.
        DiagnosticRegistry registry = LoadRegistry();
        string descriptorFormat = DiagnosticDescriptors.CanonicalHelpLinkFormat;
        descriptorFormat.ShouldBe(registry.CanonicalHelpLinkFormat,
            "DiagnosticDescriptors.CanonicalHelpLinkFormat must mirror the registry's canonicalHelpLinkFormat exactly (single source of truth, AC13).");

        // Stub help-link autolinks must also derive from this format. Pin the stubs touched by
        // Story 11.2 (which received fresh prose) as the contract anchor.
        DirectoryInfo docsRoot = DiagnosticsDocsRoot();
        Dictionary<string, DiagnosticEntry> byId = registry.Diagnostics.ToDictionary(d => d.Id, Ordinal);
        foreach (string id in new[] { "HFC0001", "HFC1037", "HFC1040", "HFC1044", "HFC1056", "HFC1057", "HFC1601", "HFC4001" }) {
            byId.ShouldContainKey(id);
            string expectedLink = string.Format(CultureInfo.InvariantCulture, descriptorFormat, id);
            string stubText = File.ReadAllText(Path.Combine(docsRoot.FullName, $"{id}.md"), Encoding.UTF8);
            stubText.ShouldContain(expectedLink, customMessage: $"{id} stub must autolink the canonical help link '{expectedLink}'.");
        }
    }

    [Fact]
    public void Severity_HFC1037_HFC1040_HFC1044_AreTablePinnedAcrossChannels() {
        // Story 11.2 code-review P5 (AC30): the override-duplicate family's severity asymmetry
        // (Error / Warning / Error) must be locked by a table-driven test so an accidental
        // copy-paste regression is caught immediately. Table covers: registry compilerSeverity,
        // suppression policy, owner package, lifecycle note presence requirement.
        DiagnosticRegistry registry = LoadRegistry();
        Dictionary<string, DiagnosticEntry> byId = registry.Diagnostics.ToDictionary(d => d.Id, Ordinal);

        (string Id, string Package, string CompilerSeverity, string SuppressionPolicy)[] expected = [
            ("HFC1037", "SourceTools", "Error", "discouraged-error"),
            ("HFC1040", "SourceTools", "Warning", "allowed-with-rationale"),
            ("HFC1044", "SourceTools", "Error", "discouraged-error"),
        ];
        foreach ((string id, string package, string severity, string policy) in expected) {
            byId.ShouldContainKey(id);
            DiagnosticEntry entry = byId[id];
            entry.OwnerPackage.ShouldBe(package, $"{id} ownerPackage drift.");
            entry.CompilerSeverity.ShouldBe(severity, $"{id} compilerSeverity drift — severity asymmetry is intentional and pinned.");
            entry.SuppressionPolicy.ShouldBe(policy, $"{id} suppressionPolicy drift.");
        }

        // The Warning member of the family must carry a lifecycleNote documenting why the
        // asymmetry is intentional (per AC22).
        byId["HFC1040"].LifecycleNote.ShouldNotBeNullOrWhiteSpace(
            "HFC1040 must carry a lifecycleNote explaining the Level-3 Warning asymmetry vs HFC1037/HFC1044 (AC22).");
    }

    [Fact]
    public void RegistryValidator_CrossPackageRangeException_NegativeControl() {
        // Story 11.2 code-review P4 (AC29): the cross-package-range exception list is pinned to
        // HFC1601 by the current validator. Negative control: dropping the exception entry must
        // cause the registry to still pass overall (because HFC1601's numericId 1601 is naturally
        // inside SourceTools range 1000-1999), proving that the exception data is provenance
        // metadata rather than a load-bearing bypass. If a future cross-package id appears that
        // IS out of its ownerPackage range, the validator must learn to read the exception's
        // id field rather than hardcoded literal — that follow-up is tracked separately.
        JsonObject pristine = RegistryJson().DeepClone().AsObject();
        string[] pristineCategories = ValidateRegistryJson(pristine).ToArray();
        pristineCategories.ShouldNotContain("out-of-range-id", "HFC1601 is in SourceTools range; pristine registry must pass range check.");

        JsonObject negative = pristine.DeepClone().AsObject();
        negative["allowedExceptions"]!["crossPackageRange"] = new JsonArray();
        string[] negativeCategories = ValidateRegistryJson(negative).ToArray();
        negativeCategories.ShouldNotContain("out-of-range-id",
            "Removing the exception entry must not change the range-check outcome for HFC1601 (it is naturally in range). The exception is provenance metadata; if it ever needs to gate an actually out-of-range id, the validator must check exception.id, not a hardcoded literal.");
    }

    [Fact]
    public void RegistryValidator_DeterministicUnderShuffledInput() {
        // Story 11.2 code-review P19 (AC25/AC31/D17): governance output must be byte-stable
        // across input order. Shuffle the diagnostics array and confirm that the validator
        // emits the same set of categories, regardless of the order in which it encounters them.
        JsonObject pristine = RegistryJson().DeepClone().AsObject();
        // Inject two duplicate-id collisions so we have something to detect deterministically.
        JsonArray diagnostics = pristine["diagnostics"]!.AsArray();
        diagnostics.Add(diagnostics[0]!.DeepClone());
        diagnostics.Add(diagnostics[1]!.DeepClone());

        HashSet<string> orderedCategories = ValidateRegistryJson(pristine).OrderBy(c => c, Ordinal).ToHashSet(Ordinal);

        Random rng = new(20260511);
        for (int trial = 0; trial < 4; trial++) {
            JsonObject shuffled = pristine.DeepClone().AsObject();
            JsonArray shuffledDiagnostics = shuffled["diagnostics"]!.AsArray();
            JsonNode[] reorder = [.. shuffledDiagnostics.Where(n => n is not null).Cast<JsonNode>()];
            shuffledDiagnostics.Clear();
            foreach (JsonNode node in reorder.OrderBy(_ => rng.Next())) {
                shuffledDiagnostics.Add(node.DeepClone());
            }

            HashSet<string> shuffledCategories = ValidateRegistryJson(shuffled).OrderBy(c => c, Ordinal).ToHashSet(Ordinal);
            shuffledCategories.SetEquals(orderedCategories).ShouldBeTrue(
                $"Validator category set drifted under shuffled input (trial {trial}); output must be byte-stable independent of JSON order (AC25/AC31).");
        }
    }

    [Fact]
    public void Story112_LedgerRowsMapToOneOfThreeFinalStates() {
        // Story 11.2 code-review P21 (AC34/D18): every Story-9-4 row routed to Story 11.2 must
        // map to exactly one of AC33's three final states (closed-with-evidence /
        // deferred-to-named-story / rejected-with-rationale). This is a direct assertion that
        // pairs with the prose ledger so that drift is caught programmatically, not by review.
        FileInfo ledger = new(Path.Combine(ProjectRoot().FullName, "_bmad-output", "implementation-artifacts", "deferred-work.md"));
        ledger.Exists.ShouldBeTrue("deferred-work.md must exist for ledger-mapping assertion.");

        string text = File.ReadAllText(ledger.FullName, Encoding.UTF8);
        Regex story112RowRegex = Story112LedgerRowRegex();
        MatchCollection matches = story112RowRegex.Matches(text);
        matches.Count.ShouldBeGreaterThan(0, "expected at least one DEF-9-4-* row in deferred-work.md.");

        string[] mandatedStates = ["Resolved", "Owner: Story", "Duplicate of", "Superseded by", "Rejected with rationale", "Split parent"];
        foreach (Match match in matches) {
            string reconciliation = match.Groups["reconciliation"].Value;
            bool hasState = mandatedStates.Any(state => reconciliation.Contains(state, StringComparison.Ordinal));
            hasState.ShouldBeTrue(
                $"DEF-9-4 ledger row '{match.Groups["id"].Value}' must carry one of AC33's mandated final-state markers (Resolved / Owner / Duplicate of / Superseded by / Rejected with rationale / Split parent); got reconciliation: '{reconciliation}'.");
        }
    }

    [Fact]
    public void SourceToolsAnalyzerReleaseRows_AllHaveBackingDescriptor() {
        // Story 11.2 code-review P31: the RS2002 NoWarn was removed from the SourceTools
        // project assuming only HFCM rows tripped it. Add an explicit guard: every
        // SourceTools release-row id must have a backing DiagnosticDescriptor field, otherwise
        // build under TreatWarningsAsErrors will break. The Shell project has its own
        // AnalyzerReleases file owned by Shell-side descriptors; only the SourceTools-owned
        // file is checked here because the SourceTools project is the one whose RS2002 was
        // removed by Story 11.2.
        HashSet<string> descriptorIds = DiagnosticDescriptorFields().Select(d => d.Id).ToHashSet(Ordinal);
        Regex strictHfcId = StrictHfcIdRegex();
        foreach (string releaseRow in SourceToolsReleaseRows().Where(id => strictHfcId.IsMatch(id))) {
            descriptorIds.ShouldContain(releaseRow,
                $"SourceTools AnalyzerReleases row '{releaseRow}' must have a backing DiagnosticDescriptor field; otherwise RS2002 will fire under TreatWarningsAsErrors=true.");
        }
    }

    private static IEnumerable<string> SourceToolsReleaseRows() {
        string[] sourceToolsReleaseFiles = [
            Path.Combine(ProjectRoot().FullName, "src", "Hexalith.FrontComposer.SourceTools", "AnalyzerReleases.Unshipped.md"),
            Path.Combine(ProjectRoot().FullName, "src", "Hexalith.FrontComposer.SourceTools", "AnalyzerReleases.Shipped.md"),
        ];
        Regex tableRowId = ReleaseRowIdRegex();
        foreach (string file in sourceToolsReleaseFiles.Where(File.Exists)) {
            foreach (string line in File.ReadAllLines(file, Encoding.UTF8)) {
                Match match = tableRowId.Match(line);
                if (match.Success) {
                    yield return match.Groups["id"].Value;
                }
            }
        }
    }

    [Fact]
    public void UnsafeGeneratedFrontMatter_HasValidator() {
        // Story 11.2 code-review P22: the unsafe-generated-front-matter sample documents a
        // category that previously had no validator. Provide a minimal validator that rejects
        // formula-injection lead chars and forbidden patterns in front-matter input, then
        // round-trip a positive and negative case so the contract is testable.
        string safeFrontMatter = "id: HFC1058\ntitle: \"Sample\"\nlifecycle: active";
        ValidateGeneratedFrontMatter(safeFrontMatter).ShouldBeEmpty();

        string formulaInjection = "=cmd|'/c calc'!A1\nid: HFC1058";
        ValidateGeneratedFrontMatter(formulaInjection).ShouldContain("frontmatter-formula-injection");

        string scriptInjection = "id: HFC1058\nnotes: <script>alert(1)</script>";
        ValidateGeneratedFrontMatter(scriptInjection).ShouldContain("frontmatter-injection-pattern");

        string nbspScript = "id: HFC1058\nnotes: < script>alert(1)</script>";
        ValidateGeneratedFrontMatter(nbspScript).ShouldContain("frontmatter-injection-pattern",
            customMessage: "NBSP-disguised script tag must be caught after StripZeroWidthAndFormatChars (P30).");
    }

    private static IEnumerable<string> ValidateGeneratedFrontMatter(string frontMatter) {
        char[] forbiddenLeads = ['=', '+', '@'];
        string[] forbiddenPatterns = ["<script", "</script", "<iframe", "javascript:", "data:text/html", "vbscript:"];

        foreach (string line in frontMatter.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)) {
            if (line.Length > 0 && forbiddenLeads.Contains(line[0])) {
                yield return "frontmatter-formula-injection";
                break;
            }
        }

        string sanitized = StripZeroWidthAndFormatChars(frontMatter);
        foreach (string forbidden in forbiddenPatterns) {
            if (sanitized.Contains(forbidden, StringComparison.OrdinalIgnoreCase)) {
                yield return "frontmatter-injection-pattern";
                yield break;
            }
        }
    }

    [GeneratedRegex(@"\*\*(?<id>DEF-9-4-[A-Z0-9]+)[^*]*\*\*[^\n]*?Reconciliation:\s*(?<reconciliation>[^\n]+)", RegexOptions.CultureInvariant | RegexOptions.Multiline)]
    private static partial Regex Story112LedgerRowRegex();

    private static IEnumerable<string> ValidateRegistryJson(JsonObject json) {
        if (!TryGetString(json, "schemaVersion", out string? schemaVersion)
            || schemaVersion != SupportedSchemaVersion) {
            yield return "unsupported-schema";
            yield break;
        }

        if (json["diagnostics"] is not JsonArray diagnosticsArray) {
            yield return "missing-diagnostics-array";
            yield break;
        }

        if (json["externalBoundaries"] is not JsonArray boundariesArray || boundariesArray.Count == 0) {
            // Story 11.2 code-review P9: empty externalBoundaries must fail closed, not pass silently.
            yield return "missing-external-boundaries";
        } else {
            HashSet<string> boundaryPackages = new(Ordinal);
            foreach (JsonNode? node in boundariesArray) {
                if (node is not JsonObject boundary
                    || !TryGetString(boundary, "package", out string? package)
                    || !TryGetString(boundary, "rangePolicy", out string? rangePolicy)
                    || !TryGetString(boundary, "provenance", out _)
                    || !TryGetString(boundary, "updatePolicy", out _)
                    || !TryGetString(boundary, "rationale", out _)) {
                    yield return "invalid-external-boundary";
                    // Story 11.2 code-review P11: a malformed row must not leak its (possibly null)
                    // package into the duplicate-tracking set; bail before recording it.
                    continue;
                }

                if (!boundaryPackages.Add(package!)) {
                    yield return "invalid-external-boundary";
                    continue;
                }

                if (package is not ("Hexalith.EventStore" or "Hexalith.Tenants")) {
                    yield return "invalid-external-boundary";
                    continue;
                }

                if (package == "Hexalith.Tenants" && rangePolicy != "no-range-reserved") {
                    yield return "invalid-external-boundary";
                }
            }

            // Story 11.2 code-review P9: every required external boundary must be declared.
            if (!boundaryPackages.Contains("Hexalith.EventStore") || !boundaryPackages.Contains("Hexalith.Tenants")) {
                yield return "missing-external-boundaries";
            }
        }

        (int Start, int End)[] ranges = ExpectedRangeBounds;
        string[] orderedOwners = ["Contracts", "SourceTools", "Shell", "EventStore", "Mcp", "Aspire"];
        HashSet<string> crossPackageExceptions = [];
        if (json["allowedExceptions"] is JsonObject exceptions
            && exceptions["crossPackageRange"] is JsonArray crossPackageRange) {
            foreach (JsonNode? node in crossPackageRange) {
                if (node is not JsonObject exception
                    || !TryGetString(exception, "id", out string? id)
                    || !TryGetString(exception, "ownerPackage", out string? ownerPackage)
                    || !TryGetString(exception, "consumingPackage", out string? consumingPackage)
                    || !TryGetString(exception, "numericRangeOwner", out string? numericRangeOwner)
                    || !TryGetString(exception, "helpLinkUri", out string? helpLinkUri)
                    || !TryGetString(exception, "reason", out _)
                    || !TryGetString(exception, "approvingStory", out _)
                    || !TryGetString(exception, "introducedIn", out _)
                    || !TryGetString(exception, "approvedOn", out _)
                    || id != "HFC1601"
                    || ownerPackage != "SourceTools"
                    || consumingPackage != "Shell"
                    || numericRangeOwner != "SourceTools"
                    || helpLinkUri != string.Format(CultureInfo.InvariantCulture, CanonicalHelpLinkFormat, id)) {
                    yield return "invalid-cross-package-exception";
                    continue;
                }

                crossPackageExceptions.Add(id);
            }
        }

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
            } else if (!IsCanonicalDocsSlug(slug, id)) {
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
                if ((numericId < start || numericId > end) && !crossPackageExceptions.Contains(id)) {
                    yield return "out-of-range-id";
                }
            }
        }
    }

    private static IEnumerable<string> ValidateCompatibilitySuppressionsJson(JsonObject json) {
        if (!TryGetString(json, "schemaVersion", out string? schemaVersion)
            || schemaVersion != SupportedSchemaVersion) {
            yield return "compatibility-unsupported-schema";
            yield break;
        }

        if (json["suppressions"] is not JsonArray suppressions) {
            yield return "compatibility-missing-suppressions";
            yield break;
        }

        HashSet<string> registryIds = LoadRegistry().Diagnostics.Select(d => d.Id).ToHashSet(Ordinal);
        HashSet<(string Package, string Tfm, string OldSignature, string HfcId, string TargetRelease)> uniqueRows = [];
        HashSet<string> allowedReasons = new(Ordinal) {
            "intentional-major-break",
            "known-binary-compatibility-gap",
            "temporary-release-candidate-exception",
        };

        foreach (JsonNode? node in suppressions) {
            if (node is not JsonObject row) {
                yield return "suppression-invalid-row";
                continue;
            }

            if (!TryGetString(row, "package", out string? package) || string.IsNullOrWhiteSpace(package)) {
                yield return "suppression-missing-package";
                continue;
            }

            if (package.Contains('*', StringComparison.Ordinal) || package.EndsWith(".*", StringComparison.Ordinal)) {
                yield return "suppression-wildcard-scope";
            }

            // Story 11.2 code-review P8: yield named categories instead of throwing Shouldly assertions
            // when a real-world row is missing required fields. Story 11.2 code-review P26: wildcard tfm
            // is rejected as a wildcard scope just like wildcard package.
            if (!TryGetString(row, "tfm", out string? tfm)) {
                yield return "suppression-missing-tfm";
                continue;
            }
            if (tfm!.Contains('*', StringComparison.Ordinal)) {
                yield return "suppression-wildcard-scope";
            }
            if (!TryGetString(row, "oldSignature", out string? oldSignature)) {
                yield return "suppression-missing-old-signature";
                continue;
            }
            if (!TryGetString(row, "newState", out string? newState)) {
                yield return "suppression-missing-new-state";
                continue;
            }
            if (!TryGetString(row, "hfcId", out string? hfcId)) {
                yield return "suppression-missing-hfc-id";
                continue;
            }
            if (!TryGetString(row, "targetRelease", out string? targetRelease)) {
                yield return "suppression-missing-target-release";
                continue;
            }
            if (!TryGetString(row, "reviewerRationale", out string? rationale)) {
                yield return "suppression-missing-reviewer-rationale";
                continue;
            }
            if (!TryGetString(row, "ownerStory", out string? ownerStory)) {
                yield return "suppression-missing-owner-story";
                continue;
            }
            if (!TryGetString(row, "expiresAfter", out string? expiresAfter)) {
                yield return "suppression-missing-expires-after";
                continue;
            }
            if (!TryGetString(row, "reason", out string? reason)) {
                yield return "suppression-missing-reason";
                continue;
            }

            if (!registryIds.Contains(hfcId!)) {
                yield return "suppression-unknown-diagnostic";
            }

            if (!allowedReasons.Contains(reason!)) {
                yield return "suppression-unknown-reason";
            }

            // Story 11.2 code-review P8: malformed version literals must yield a named category
            // instead of throwing FormatException/ArgumentException out of the iterator.
            if (!TryParseVersionToken(expiresAfter!, out Version? parsedExpires)
                || !TryParseVersionToken(targetRelease!, out Version? parsedTarget)) {
                yield return "suppression-malformed-version";
            } else if (parsedExpires! <= parsedTarget!) {
                yield return "suppression-expired";
            }

            if (string.IsNullOrWhiteSpace(newState) || string.IsNullOrWhiteSpace(rationale) || string.IsNullOrWhiteSpace(ownerStory)) {
                yield return "suppression-missing-required-field";
            }

            if (!uniqueRows.Add((package, tfm!, oldSignature!, hfcId!, targetRelease!))) {
                yield return "suppression-duplicate-row";
            }
        }
    }

    private static bool TryParseVersionToken(string value, out Version? version) {
        version = null;
        if (string.IsNullOrWhiteSpace(value)) {
            return false;
        }

        try {
            version = ParseVersionToken(value);
            return true;
        } catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException) {
            return false;
        }
    }

    private static bool TryGetString(JsonObject json, string propertyName, out string? value) {
        value = null;
        if (json[propertyName] is not JsonValue jsonValue) {
            return false;
        }

        try {
            value = jsonValue.GetValue<string>();
            return !string.IsNullOrWhiteSpace(value);
        } catch (Exception ex) when (ex is InvalidOperationException or FormatException) {
            return false;
        }
    }

    private static bool IsCanonicalDocsSlug(string slug, string id) {
        if (slug != slug.Normalize(NormalizationForm.FormC)
            || slug.Any(char.IsWhiteSpace)
            || slug.Any(IsConfusableOrFormatChar)
            || slug.Contains('\\', StringComparison.Ordinal)
            || slug.Contains('?', StringComparison.Ordinal)
            || slug.Contains('#', StringComparison.Ordinal)
            || slug.Contains("..", StringComparison.Ordinal)
            || IsRootedSlug(slug)) {
            // Story 11.2 code-review P7: replace Path.IsPathRooted with a portable check so that
            // strings like "C:/diagnostics/HFC1058" are rejected on Linux CI runners as well.
            return false;
        }

        string decoded;
        try {
            decoded = Uri.UnescapeDataString(slug);
        } catch (UriFormatException) {
            return false;
        }

        if (decoded.Contains('%', StringComparison.Ordinal)
            || decoded.Contains('\\', StringComparison.Ordinal)
            || decoded.Contains("..", StringComparison.Ordinal)
            || decoded != slug) {
            return false;
        }

        return decoded == $"diagnostics/{id}";
    }

    private static Version ParseVersionToken(string value) {
        string trimmed = value.Trim().TrimStart('v', 'V');
        return ParseVersion(trimmed);
    }

    private static bool IsConfusableOrFormatChar(char value) {
        if (char.GetUnicodeCategory(value) == System.Globalization.UnicodeCategory.Format) {
            return true;
        }

        // Story 11.2 code-review P30: NBSP (U+00A0) and other non-format whitespace must be
        // stripped before injection-pattern scanning so that `< script` or `< script` is
        // caught after normalization.
        if (value == ' ' || value == ' ' || value == ' ' || value == '　') {
            return true;
        }

        return value is '​' or '‌' or '‍' or '﻿'
            or '‎' or '‏' or '⁠' or '᠎'
            or '‪' or '‫' or '‬' or '‭' or '‮'
            or 'Ｈ';
    }

    private static bool IsRootedSlug(string slug) {
        if (string.IsNullOrEmpty(slug)) {
            return false;
        }

        // Unix-style rooted (`/diagnostics/...`) — rooted on every platform.
        if (slug[0] == '/' || slug[0] == '\\') {
            return true;
        }

        // Windows drive-letter rooted (`C:/diagnostics/...` or `c:\diagnostics\...`) — rooted
        // semantically even on Linux CI; reject portably.
        if (slug.Length >= 2 && slug[1] == ':' && char.IsLetter(slug[0])) {
            return true;
        }

        return false;
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

    // Story 11.2 code-review P25: pin the diagnostics URL shape to https-only canonical form
    // (no trailing slash, no query, no fragment). Captures both the scheme and host so
    // SourceHfcIds_AreRegistryBackedAndDiagnosticLinksUseCanonicalHost can fail closed on http://
    // downgrades, alternative hosts, or appended ?/# variants.
    [GeneratedRegex(@"(?<scheme>https?)://(?<host>[A-Za-z0-9.\-]+)/[^""'\s?#]*diagnostics/HFC[0-9]{4}(?<suffix>[/?#][^""'\s]*)?", RegexOptions.CultureInvariant)]
    private static partial Regex DiagnosticsDocsLinkUrlRegex();

    [GeneratedRegex(@"^[0-9]+\.[0-9]+(?:\.[0-9]+)?(?:[-+][0-9A-Za-z.\-]+)?$", RegexOptions.CultureInvariant)]
    private static partial Regex SemverShapeRegex();

    [GeneratedRegex(@"^(?:HFCM?-PLACEHOLDER|HFCM?[0-9]{4})$", RegexOptions.CultureInvariant)]
    private static partial Regex SampleFindingIdShapeRegex();

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
        DiagnosticExternalBoundary[] ExternalBoundaries,
        DiagnosticAllowedExceptions AllowedExceptions,
        DiagnosticEntry[] Diagnostics);

    private sealed record DiagnosticRange(
        string OwnerPackage,
        int Start,
        int End);

    private sealed record DiagnosticExternalBoundary(
        string Package,
        string Owner,
        string RangePolicy,
        string? RangeOwner,
        string Provenance,
        string UpdatePolicy,
        string Rationale);

    private sealed record DiagnosticAllowedExceptions(
        CrossPackageRangeException[] CrossPackageRange);

    private sealed record CrossPackageRangeException(
        string Id,
        string OwnerPackage,
        string ConsumingPackage,
        string NumericRangeOwner,
        string HelpLinkUri,
        string Reason,
        string ApprovingStory,
        string IntroducedIn,
        string ApprovedOn);

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
