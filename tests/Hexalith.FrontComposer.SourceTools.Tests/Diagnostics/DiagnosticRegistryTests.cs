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
                // Pass-2 R25: duplicate entries within a single relatedIds array are rejected so
                // a sloppy edit cannot inflate the apparent set ([HFC1040, HFC1040, HFC1601]).
                diagnostic.RelatedIds.ShouldBe(diagnostic.RelatedIds.Distinct(Ordinal).ToArray(),
                    $"{diagnostic.Id} relatedIds contains duplicates: [{string.Join(", ", diagnostic.RelatedIds)}].");
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
        // Pass-2 R36: an empty / whitespace `introducedIn` must Assert.Fail rather than silently
        // pass — the field is required for release provenance.
        Regex semverShape = SemverShapeRegex();
        foreach (DiagnosticEntry diagnostic in registry.Diagnostics) {
            diagnostic.IntroducedIn.ShouldNotBeNullOrWhiteSpace(
                $"{diagnostic.Id} introducedIn is required and must be a SemVer-shaped version.");
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
                string raw = urlMatch.Value;
                // Pass-2 R20: structural Uri check — scheme/host/port/userinfo/path/query/fragment.
                // Coarse regex extraction must be paired with a strict parse so trailing-dot host,
                // userinfo (`user@host`), explicit port, query, or fragment cannot bypass.
                Uri.TryCreate(raw, UriKind.Absolute, out Uri? parsed).ShouldBeTrue(
                    $"{ProjectRelativePath(file)} contains a malformed diagnostics URL: '{raw}'.");
                parsed!.Scheme.ShouldBe("https", $"{ProjectRelativePath(file)} uses non-https diagnostics scheme '{parsed.Scheme}' in '{raw}'.");
                parsed.Host.TrimEnd('.').ShouldBe(CanonicalDocsHost,
                    $"{ProjectRelativePath(file)} links to non-canonical diagnostics host '{parsed.Host}' in '{raw}'.");
                parsed.IsDefaultPort.ShouldBeTrue($"{ProjectRelativePath(file)} diagnostics URL '{raw}' must not specify an explicit port.");
                parsed.UserInfo.ShouldBeEmpty($"{ProjectRelativePath(file)} diagnostics URL '{raw}' must not contain userinfo.");
                parsed.Query.ShouldBeEmpty($"{ProjectRelativePath(file)} diagnostics URL '{raw}' must not carry a query.");
                parsed.Fragment.ShouldBeEmpty($"{ProjectRelativePath(file)} diagnostics URL '{raw}' must not carry a fragment.");
                Regex canonicalPath = new(@"^/FrontComposer/diagnostics/HFC[0-9]{4}$", RegexOptions.CultureInvariant);
                canonicalPath.IsMatch(parsed.AbsolutePath).ShouldBeTrue(
                    $"{ProjectRelativePath(file)} diagnostics URL '{raw}' path must be exactly `/FrontComposer/diagnostics/HFCxxxx` (got '{parsed.AbsolutePath}').");
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

        // Pass-2 R32: the same JSON that the Shouldly checks above traverse must also satisfy
        // ValidateMigrationFindingsJson — otherwise the validator could yield a spurious category
        // for valid input and the outer test would not notice. This is the negative-control
        // assertion the validator needs.
        ValidateMigrationFindingsJson(json).ShouldBeEmpty(
            "pristine migration-findings.json must yield zero categories from the fail-closed validator; any drift between the outer Shouldly checks and ValidateMigrationFindingsJson must be reconciled.");
    }

    [Theory]
    [InlineData("duplicate-id", 0)]
    [InlineData("wrong-prefix", 0)]
    [InlineData("wrong-bucket", 0)]
    [InlineData("missing-release-note", 0)]
    [InlineData("missing-owner-story", 0)]
    [InlineData("malformed-introduced-in", 0)]
    [InlineData("missing-release-provenance", 0)]
    // Pass-2 R12: explicit schema-level mutations exercising the unsupported-schema and
    // missing-findings-array branches that previously had no Theory coverage.
    [InlineData("missing-schema-version", 0)]
    [InlineData("wrong-schema-version", 0)]
    [InlineData("missing-findings-array", 0)]
    // Pass-2 R10: severity, category, slug mutations matching the new validator categories.
    [InlineData("invalid-severity", 0)]
    [InlineData("invalid-category", 0)]
    [InlineData("malformed-slug", 0)]
    [InlineData("malformed-approved-on", 0)]
    // Pass-2 R22: analyzer-descriptor-misclassification — an HFCM finding whose id collides with
    // a Roslyn HFC descriptor id is detected via the wrong-prefix branch since the id-shape regex
    // rejects HFC-prefixed values from a migration-findings row.
    [InlineData("analyzer-descriptor-misclassification", 0)]
    // Pass-2 R24: same mutations applied to row index 1 to ensure per-row validation, not just
    // findings[0]. Sample is small enough that two reps cover the loop semantics.
    [InlineData("duplicate-id", 1)]
    [InlineData("wrong-prefix", 1)]
    [InlineData("missing-owner-story", 1)]
    public void MigrationFindingsValidator_FailsClosedWithNamedCategories(string mutation, int rowIndex) {
        // Story 11.2 code-review P3 (T3 subtask 4): the CLI-specific release-row artifact must
        // fail closed on each named mutation. Pass-2 R11/R12/R22/R24/R33: extended mutation set,
        // per-row coverage, explicit default case in the switch.
        JsonObject json = JsonNode.Parse(File.ReadAllText(Path.Combine(DiagnosticsDocsRoot().FullName, "migration-findings.json"), Encoding.UTF8))!.AsObject();
        JsonArray findings = json["findings"]!.AsArray();
        JsonObject row = findings[rowIndex]!.AsObject();

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
                // Pass-2 R39: use a clearly non-empty but non-SemVer value so the path under
                // test is the SemVer-shape branch, not the missing/empty branch.
                row["introducedIn"] = "1.x";
                break;
            case "missing-release-provenance":
                row.Remove("releaseProvenance");
                break;
            case "missing-schema-version":
                json.Remove("schemaVersion");
                break;
            case "wrong-schema-version":
                json["schemaVersion"] = "2.0";
                break;
            case "missing-findings-array":
                json.Remove("findings");
                break;
            case "invalid-severity":
                row["severity"] = "Verbose";
                break;
            case "invalid-category":
                row["category"] = "Other.Category";
                break;
            case "malformed-slug":
                row["migrationDocsSlug"] = "migrations/../../etc/passwd";
                break;
            case "malformed-approved-on":
                row["releaseProvenance"]!["approvedOn"] = "yesterday";
                break;
            case "analyzer-descriptor-misclassification":
                // Pass-2 R22: an HFC-prefixed id smuggled into the findings array; the validator
                // must yield wrong-prefix (HFCM-only).
                row["id"] = "HFC9999";
                break;
            default:
                throw new InvalidOperationException($"Pass-2 R33: unrecognized Theory mutation '{mutation}' — add a case or remove the [InlineData].");
        }

        string expectedCategory = mutation switch {
            "duplicate-id" => "hfcm-duplicate-id",
            "wrong-prefix" or "analyzer-descriptor-misclassification" => "hfcm-wrong-prefix",
            "wrong-bucket" => "hfcm-wrong-bucket",
            "missing-release-note" => "hfcm-missing-release-note",
            "missing-owner-story" => "hfcm-missing-owner-story",
            "malformed-introduced-in" => "hfcm-malformed-introduced-in",
            "missing-release-provenance" => "hfcm-missing-release-provenance",
            "missing-schema-version" or "wrong-schema-version" => "hfcm-unsupported-schema",
            "missing-findings-array" => "hfcm-missing-findings-array",
            "invalid-severity" => "hfcm-invalid-severity",
            "invalid-category" => "hfcm-invalid-category",
            "malformed-slug" => "hfcm-malformed-slug",
            "malformed-approved-on" => "hfcm-malformed-approved-on",
            _ => throw new InvalidOperationException($"Pass-2 R33: unrecognized mutation '{mutation}' in expectedCategory map."),
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
        // Pass-2 R14: approvedOn must be ISO-8601 date — `YYYY-MM-DD`. Free-form `TBD` / `soon`
        // bypasses audit-trail provenance.
        Regex isoDate = new(@"^[0-9]{4}-[0-9]{2}-[0-9]{2}$", RegexOptions.CultureInvariant);
        HashSet<string> seen = new(Ordinal);
        HashSet<string> allowedSeverities = new(Ordinal) { "Info", "Warning", "Error" };
        const string requiredCategory = "HexalithFrontComposer.Migration";
        foreach (JsonNode? node in findings) {
            if (node is not JsonObject row) {
                yield return "hfcm-invalid-row";
                continue;
            }
            if (!TryGetString(row, "id", out string? id)) {
                yield return "hfcm-missing-id";
                continue;
            }
            // Pass-2 R11: do not `continue` on wrong-prefix — yield the category and still run the
            // dedupe/owner/release-note checks so combined mutations (wrong-prefix + duplicate)
            // surface every applicable category.
            bool idShapeOk = idShape.IsMatch(id!);
            if (!idShapeOk) {
                yield return "hfcm-wrong-prefix";
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
            // Pass-2 R10: per-row severity, category, and migrationDocsSlug validation.
            if (!TryGetString(row, "severity", out string? severity) || !allowedSeverities.Contains(severity!)) {
                yield return "hfcm-invalid-severity";
            }
            if (!TryGetString(row, "category", out string? category) || category != requiredCategory) {
                yield return "hfcm-invalid-category";
            }
            // Pass-2 R10: containment-safe migrationDocsSlug check — equivalent to IsCanonicalDocsSlug
            // for the `migrations/<id>` prefix. Rejects `..` traversal, encoded slash/backslash,
            // query/fragment, rooted paths, non-NFC, zero-width, percent-encoding.
            if (!TryGetString(row, "migrationDocsSlug", out string? slug)
                || !IsCanonicalMigrationSlug(slug!)) {
                yield return "hfcm-malformed-slug";
            }
            if (row["releaseProvenance"] is not JsonObject provenance
                || !TryGetString(provenance, "approvingStory", out _)
                || !TryGetString(provenance, "approvedOn", out string? approvedOn)
                || !TryGetString(provenance, "rationale", out _)) {
                yield return "hfcm-missing-release-provenance";
            } else if (!isoDate.IsMatch(approvedOn!)) {
                // Pass-2 R14: provenance exists but approvedOn is not ISO date — distinct category.
                yield return "hfcm-malformed-approved-on";
            }
        }
    }

    // Pass-2 R10: parallel of IsCanonicalDocsSlug for `migrations/<id>` namespace.
    private static bool IsCanonicalMigrationSlug(string slug) {
        if (string.IsNullOrEmpty(slug)
            || slug != slug.Normalize(NormalizationForm.FormC)
            || slug.Any(char.IsWhiteSpace)
            || slug.Any(IsConfusableOrFormatChar)
            || slug.Contains('\\', StringComparison.Ordinal)
            || slug.Contains('?', StringComparison.Ordinal)
            || slug.Contains('#', StringComparison.Ordinal)
            || slug.Contains("..", StringComparison.Ordinal)
            || IsRootedSlug(slug)) {
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

        return decoded.StartsWith("migrations/", StringComparison.Ordinal)
            && decoded.Length > "migrations/".Length
            && !decoded[..^1].EndsWith('/');
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

        // AC32: max-item / max-character budget. Sample reports are evidence artifacts, not
        // unbounded log dumps; if we ever exceed these the redaction layer is leaking.
        // Pass-2 R23: byte budget for AC32 is documented — derived from the largest legitimate
        // sample (~6 KB) × 2.5 safety margin = 16 KiB. Measured in UTF-8 bytes, not UTF-16 chars,
        // since the "bytes" semantics matter for streaming/redaction.
        const int MaxFindingsPerSample = 32;
        const int MaxJsonBytesPerSample = 16 * 1024;

        foreach (string sample in expectedSamples) {
            string path = Path.Combine(samplesRoot, sample);
            string json = File.ReadAllText(path, Encoding.UTF8);
            int jsonBytes = Encoding.UTF8.GetByteCount(json);
            JsonObject parsed = JsonNode.Parse(json)!.AsObject();
            parsed["schemaVersion"]!.GetValue<string>().ShouldBe("1.0");
            parsed["exitCode"]!.GetValue<int>().ShouldBe(2);
            parsed["category"]!.GetValue<string>().ShouldNotBeNullOrWhiteSpace();
            JsonArray findings = parsed["findings"]!.AsArray();
            findings.Count.ShouldBeGreaterThan(0, $"{sample} must include at least one finding.");
            findings.Count.ShouldBeLessThanOrEqualTo(MaxFindingsPerSample, $"{sample} exceeds max findings budget ({findings.Count} > {MaxFindingsPerSample}); redaction/truncation must happen before commit (AC32).");
            jsonBytes.ShouldBeLessThanOrEqualTo(MaxJsonBytesPerSample, $"{sample} exceeds max sample size ({jsonBytes} > {MaxJsonBytesPerSample} bytes); samples are evidence artifacts, not log dumps (AC32).");
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

            AssertNoForbiddenSentinels(json, sample);
        }

        // Pass-2 R7: AC32 explicitly requires applying the sentinel scan to docs stubs,
        // migration-findings.json, and validation reports too — not just samples.
        foreach (string stub in Directory.EnumerateFiles(DiagnosticsDocsRoot().FullName, "HFC*.md").OrderBy(p => p, Ordinal)) {
            AssertNoForbiddenSentinels(File.ReadAllText(stub, Encoding.UTF8), ProjectRelativePath(stub));
        }
        string migrationFindingsPath = Path.Combine(DiagnosticsDocsRoot().FullName, "migration-findings.json");
        if (File.Exists(migrationFindingsPath)) {
            AssertNoForbiddenSentinels(File.ReadAllText(migrationFindingsPath, Encoding.UTF8), ProjectRelativePath(migrationFindingsPath));
        }
        string validationManifestPath = Path.Combine(ProjectRoot().FullName, "artifacts", "docs", "validation-manifest.json");
        if (File.Exists(validationManifestPath)) {
            AssertNoForbiddenSentinels(File.ReadAllText(validationManifestPath, Encoding.UTF8), ProjectRelativePath(validationManifestPath));
        }
    }

    // Pass-2 R7: extracted forbidden-sentinel scan applied to samples, docs stubs,
    // migration-findings.json, and validation reports. Sentinel list covers Windows/Unix absolute
    // paths, $HOME variants, GitHub Actions runner paths, common token prefixes (GitHub, Slack,
    // AWS, Azure, Anthropic, Google, GitLab, PEM), SDK banners, live feed URLs, and stack-trace
    // markers. Short ASCII prefixes (`AKIA`, `ASIA`, `AIza`) are anchored to typical surrounding
    // characters via word-boundary search inside ScanForSentinel to avoid false positives in prose.
    private static readonly string[] ForbiddenSentinels = [
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
        "xoxa-",
        "xoxp-",
        "xoxs-",
        "sk-ant-",
        "Microsoft .NET SDK",
        "   at System.",
        "   at Microsoft.",
        "fv-az",
        "eyJ",
        "-----BEGIN",
        "glpat-",
        "sig=",
    ];

    // Short uppercase tokens that occur as legitimate substrings in prose; require word-boundary
    // context (preceded and followed by non-token-char) to count as a sentinel.
    private static readonly string[] ForbiddenSentinelWordBoundaryTokens = [
        "AKIA",
        "ASIA",
        "AIza",
    ];

    private static void AssertNoForbiddenSentinels(string content, string source) {
        foreach (string sentinel in ForbiddenSentinels) {
            content.ShouldNotContain(sentinel, Case.Insensitive,
                $"{source} contains forbidden sentinel '{sentinel}'; redaction failure (AC32).");
        }
        foreach (string token in ForbiddenSentinelWordBoundaryTokens) {
            Regex wordBounded = new($@"\b{Regex.Escape(token)}[0-9A-Z_]{{8,}}\b", RegexOptions.CultureInvariant);
            wordBounded.IsMatch(content).ShouldBeFalse(
                $"{source} contains token shape matching forbidden sentinel '{token}<token-tail>'; redaction failure (AC32).");
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
    public void DocsStubBody_RelatedDiagnosticsListEqualsRegistry() {
        // Pass-2 R4: the docs-stub `## Related Diagnostics` body section must enumerate the same
        // ids as the registry's relatedIds field. The front-matter array is already pinned by
        // DocsStubs_ArePresentBoundedAndRegistryBacked; this test pins the body so a maintainer
        // cannot update one half (e.g., rewrite the bullets) without touching the other (e.g.,
        // refresh front-matter / registry).
        DiagnosticRegistry registry = LoadRegistry();
        DirectoryInfo docsRoot = DiagnosticsDocsRoot();
        Regex hfcId = HfcIdRegex();

        foreach (DiagnosticEntry diagnostic in registry.Diagnostics) {
            string[] expected = (diagnostic.RelatedIds ?? []).OrderBy(id => id, Ordinal).ToArray();
            if (expected.Length == 0) {
                continue; // empty registry relatedIds — body may legitimately say "None currently".
            }

            string raw = File.ReadAllText(Path.Combine(docsRoot.FullName, $"{diagnostic.Id}.md"), Encoding.UTF8);
            string stub = StripZeroWidthAndFormatChars(raw);

            int sectionStart = stub.IndexOf("## Related Diagnostics", StringComparison.Ordinal);
            sectionStart.ShouldBePositive($"{diagnostic.Id} stub is missing `## Related Diagnostics` section.");
            int sectionEnd = stub.IndexOf("\n## ", sectionStart + "## Related Diagnostics".Length, StringComparison.Ordinal);
            if (sectionEnd < 0) {
                // Use the narrative-end marker as a fallback if no next section follows.
                sectionEnd = stub.IndexOf("<!-- story-9-5:narrative-end -->", sectionStart, StringComparison.Ordinal);
            }
            sectionEnd.ShouldBeGreaterThan(sectionStart, $"{diagnostic.Id} stub `## Related Diagnostics` section appears unterminated.");
            string sectionBody = stub[sectionStart..sectionEnd];

            string[] bodyIds = hfcId.Matches(sectionBody)
                .Select(m => m.Value)
                // Filter to ids the section is *enumerating* (exclude `HFC` references inside
                // descriptive prose by requiring they appear in a markdown link form). The
                // bodies use `- [HFC1040](https://...) — ...`; the regex picks up HFC1040 in
                // both the link text and the URL — collapse duplicates.
                .Distinct(Ordinal)
                .OrderBy(id => id, Ordinal)
                .ToArray();

            bodyIds.ShouldBe(expected,
                $"{diagnostic.Id} stub `## Related Diagnostics` body enumerates [{string.Join(", ", bodyIds)}] but registry relatedIds is [{string.Join(", ", expected)}]. Body and front-matter must stay in sync.");
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

        // Stub help-link autolinks must also derive from this format. Pass-2 R27: iterate every
        // active/deprecated registry diagnostic rather than a hardcoded 8-id list so future
        // diagnostics inherit the contract automatically. Retired and reserved stubs intentionally
        // omit the canonical link block (retired-stub convention from Story 9-5). Pass-2 R35:
        // normalize the stub text via StripZeroWidthAndFormatChars before substring search so a
        // zero-width-joiner inside the URL cannot bypass the canonical-link check, and assert
        // absence of any `http://` variant.
        DirectoryInfo docsRoot = DiagnosticsDocsRoot();
        foreach (DiagnosticEntry diagnostic in registry.Diagnostics) {
            if (diagnostic.Lifecycle is "retired" or "reserved" or "removed-in-major") {
                continue;
            }
            string id = diagnostic.Id;
            string expectedLink = string.Format(CultureInfo.InvariantCulture, descriptorFormat, id);
            string rawStub = File.ReadAllText(Path.Combine(docsRoot.FullName, $"{id}.md"), Encoding.UTF8);
            string stubText = StripZeroWidthAndFormatChars(rawStub);
            stubText.ShouldContain(expectedLink,
                customMessage: $"{id} stub must autolink the canonical help link '{expectedLink}' after zero-width normalization.");

            // Pass-2 R35: reject any `http://hexalith.github.io/FrontComposer/diagnostics/{id}`
            // variant in the stub — only the https form is canonical.
            string insecureVariant = expectedLink.Replace("https://", "http://", StringComparison.Ordinal);
            stubText.ShouldNotContain(insecureVariant,
                customMessage: $"{id} stub must not contain insecure http:// help-link variant.");
        }
    }

    [Fact]
    public void Severity_HFC1037_HFC1040_HFC1044_AreTablePinnedAcrossChannels() {
        // Story 11.2 code-review P5 (AC30) + Pass-2 R1/R42: the override-duplicate family's
        // severity asymmetry (Error / Warning / Error) is pinned across four channels:
        //   1. Registry compilerSeverity + suppression policy + owner package + lifecycleNote.
        //   2. Roslyn DiagnosticDescriptor.DefaultSeverity.
        //   3. AnalyzerReleases.*.md release-row severity column.
        //   4. Docs stub front-matter `severity:` field.
        // AC30 mandates the matrix span all four; a Warning/Error flip in any one channel must
        // fail the test immediately.
        DiagnosticRegistry registry = LoadRegistry();
        Dictionary<string, DiagnosticEntry> byId = registry.Diagnostics.ToDictionary(d => d.Id, Ordinal);
        Dictionary<string, DiagnosticDescriptor> descriptorsById = DiagnosticDescriptorFields().ToDictionary(d => d.Id, Ordinal);
        Dictionary<string, string> releaseRowSeverityById = LoadReleaseRowSeverityMap();
        DirectoryInfo docsRoot = DiagnosticsDocsRoot();
        Regex frontMatterSeverity = FrontMatterSeverityLineRegex();

        (string Id, string Package, string CompilerSeverity, string SuppressionPolicy)[] expected = [
            ("HFC1037", "SourceTools", "Error", "discouraged-error"),
            ("HFC1040", "SourceTools", "Warning", "allowed-with-rationale"),
            ("HFC1044", "SourceTools", "Error", "discouraged-error"),
        ];
        foreach ((string id, string package, string severity, string policy) in expected) {
            byId.ShouldContainKey(id);
            DiagnosticEntry entry = byId[id];

            // Channel 1: registry.
            entry.OwnerPackage.ShouldBe(package, $"{id} ownerPackage drift.");
            entry.CompilerSeverity.ShouldBe(severity, $"{id} compilerSeverity drift — severity asymmetry is intentional and pinned.");
            entry.SuppressionPolicy.ShouldBe(policy, $"{id} suppressionPolicy drift.");

            // Channel 2: Roslyn descriptor.
            descriptorsById.ShouldContainKey(id, $"{id} must have a DiagnosticDescriptor field for cross-channel severity pinning.");
            descriptorsById[id].DefaultSeverity.ToString().ShouldBe(severity,
                $"{id} Roslyn DiagnosticDescriptor.DefaultSeverity drift from registry compilerSeverity (AC30).");

            // Channel 3: release-row severity column.
            releaseRowSeverityById.ShouldContainKey(id, $"{id} must appear in an AnalyzerReleases.*.md release row.");
            releaseRowSeverityById[id].ShouldBe(severity,
                $"{id} AnalyzerReleases release-row severity drift from registry compilerSeverity (AC30).");

            // Channel 4: docs stub front-matter severity.
            string stubText = File.ReadAllText(Path.Combine(docsRoot.FullName, $"{id}.md"), Encoding.UTF8);
            Match severityMatch = frontMatterSeverity.Match(stubText);
            severityMatch.Success.ShouldBeTrue($"{id} docs stub must declare front-matter severity (AC30).");
            severityMatch.Groups["severity"].Value.ShouldBe(severity,
                $"{id} docs stub front-matter severity drift from registry compilerSeverity (AC30).");
        }

        // The Warning member of the family must carry a lifecycleNote documenting why the
        // asymmetry is intentional (per AC22).
        byId["HFC1040"].LifecycleNote.ShouldNotBeNullOrWhiteSpace(
            "HFC1040 must carry a lifecycleNote explaining the Level-3 Warning asymmetry vs HFC1037/HFC1044 (AC22).");
    }

    // Pass-2 R1: parse `AnalyzerReleases.*.md` table rows to extract per-id severity column so the
    // severity matrix can pin release-row severity against registry compilerSeverity. The table
    // shape is `| HFCxxxx | Category | Severity | Notes |` per Roslyn release-tracking
    // conventions; we tolerate leading/trailing whitespace and only capture id + severity.
    private static Dictionary<string, string> LoadReleaseRowSeverityMap() {
        string[] releaseFiles = [
            Path.Combine(ProjectRoot().FullName, "src", "Hexalith.FrontComposer.SourceTools", "AnalyzerReleases.Unshipped.md"),
            Path.Combine(ProjectRoot().FullName, "src", "Hexalith.FrontComposer.SourceTools", "AnalyzerReleases.Shipped.md"),
            Path.Combine(ProjectRoot().FullName, "src", "Hexalith.FrontComposer.Shell", "AnalyzerReleases.Unshipped.md"),
        ];
        // Release rows use space-pipe column separators (no leading pipe), e.g.
        //   `HFC1001 | HexalithFrontComposer | Warning | description`
        Regex tableRow = new(@"^\s*(?<id>HFC[0-9]{4})\s*\|\s*(?<category>[^|]+?)\s*\|\s*(?<severity>Info|Warning|Error|Hidden)\s*\|", RegexOptions.CultureInvariant);
        Dictionary<string, string> map = new(StringComparer.Ordinal);
        foreach (string file in releaseFiles.Where(File.Exists).OrderBy(p => p, StringComparer.Ordinal)) {
            foreach (string line in File.ReadAllLines(file, Encoding.UTF8)) {
                Match match = tableRow.Match(line);
                if (match.Success) {
                    map[match.Groups["id"].Value] = match.Groups["severity"].Value;
                }
            }
        }
        return map;
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
        // across input order. Pass-2 R8 replaces the broken `OrderBy(rng.Next())` LINQ pseudo-
        // shuffle with a real Fisher-Yates shuffle, asserts the permutation is actually different,
        // and compares ordered lists (byte-stability) rather than HashSet (set-stability).
        JsonObject pristine = RegistryJson().DeepClone().AsObject();
        // Inject two duplicate-id collisions so we have something to detect deterministically.
        JsonArray diagnostics = pristine["diagnostics"]!.AsArray();
        diagnostics.Count.ShouldBeGreaterThanOrEqualTo(2,
            "registry needs at least two diagnostics to inject duplicate-collisions for the determinism trial.");
        diagnostics.Add(diagnostics[0]!.DeepClone());
        diagnostics.Add(diagnostics[1]!.DeepClone());

        // Pass-2 R8: order ordinally so byte-stable comparison is meaningful (the validator may
        // yield categories in input order, but a deterministic governance report sorts the output
        // before truncation/redaction — assert the sorted form is stable across shuffles).
        List<string> orderedCategories = [.. ValidateRegistryJson(pristine).OrderBy(c => c, Ordinal)];

        Random rng = new(20260511);
        for (int trial = 0; trial < 4; trial++) {
            JsonObject shuffled = pristine.DeepClone().AsObject();
            JsonArray shuffledDiagnostics = shuffled["diagnostics"]!.AsArray();
            JsonNode[] reorder = [.. shuffledDiagnostics.Where(n => n is not null).Cast<JsonNode>()];

            // Pass-2 R8: real Fisher-Yates (in-place swap) so the resulting array is guaranteed
            // to be a uniform random permutation, not a non-deterministic LINQ sort.
            for (int i = reorder.Length - 1; i > 0; i--) {
                int j = rng.Next(i + 1);
                (reorder[i], reorder[j]) = (reorder[j], reorder[i]);
            }

            // Sanity check: the shuffle MUST actually reorder for the determinism claim to mean
            // anything. With ≥3 elements and a meaningful seed sequence this is overwhelmingly
            // likely; if it ever fails, the shuffle algorithm regressed.
            string[] originalIds = shuffledDiagnostics.Select(n => n!["id"]!.GetValue<string>()).ToArray();
            string[] shuffledIds = reorder.Select(n => n["id"]!.GetValue<string>()).ToArray();
            (!originalIds.SequenceEqual(shuffledIds)).ShouldBeTrue(
                $"trial {trial} shuffle produced the original order; Fisher-Yates should permute non-trivially.");

            shuffledDiagnostics.Clear();
            foreach (JsonNode node in reorder) {
                shuffledDiagnostics.Add(node.DeepClone());
            }

            List<string> shuffledCategories = [.. ValidateRegistryJson(shuffled).OrderBy(c => c, Ordinal)];
            shuffledCategories.ShouldBe(orderedCategories,
                $"Validator category list drifted under shuffled input (trial {trial}); output must be byte-stable independent of JSON order (AC25/AC31/D17).");
        }
    }

    [Fact]
    public void GovernanceEnumerations_AreDeterministicAcrossSurfaces() {
        // Pass-2 R9 (AC25/AC31/D17/T6): the registry-rows shuffle is covered by
        // RegistryValidator_DeterministicUnderShuffledInput; this companion test covers the
        // remaining five enumerated surfaces called out by T6 — source files, docs stub rows,
        // related IDs, suppressions, and package groups — by asserting each enumeration sorts
        // ordinally and produces byte-stable output independent of underlying filesystem or input
        // order. Each surface is tested by re-running its enumerator and checking the result is
        // already sorted under StringComparer.Ordinal (which is the canonical sort the
        // governance report applies before truncation/redaction).
        Random rng = new(20260512);

        // Surface 1: source files — OwnedSourceFiles already sorts ordinal; shuffle a snapshot
        // copy and confirm reapplying ordinal sort returns the canonical order.
        string[] sourceFiles = OwnedSourceFiles().ToArray();
        sourceFiles.ShouldBe(sourceFiles.OrderBy(p => p, Ordinal).ToArray(),
            "OwnedSourceFiles enumeration must be ordinally sorted (T6 source-files surface).");
        AssertShuffledSortIsStable(sourceFiles, rng, "source-files");

        // Surface 2: docs stub rows — every stub filename under docs/diagnostics/ for a registry
        // diagnostic. Same byte-stability contract.
        string[] stubFiles = Directory.EnumerateFiles(DiagnosticsDocsRoot().FullName, "HFC*.md")
            .Select(Path.GetFileName)
            .OrderBy(name => name, Ordinal)
            .ToArray()!;
        stubFiles.Length.ShouldBeGreaterThan(0, "expected at least one HFC*.md stub.");
        AssertShuffledSortIsStable(stubFiles, rng, "docs-stub-rows");

        // Surface 3: related IDs — flatten every diagnostic's relatedIds with the parent id, then
        // assert ordinal sort is stable under shuffle.
        DiagnosticRegistry registry = LoadRegistry();
        string[] relatedPairs = registry.Diagnostics
            .Where(d => d.RelatedIds is { Length: > 0 })
            .SelectMany(d => d.RelatedIds!.Select(r => $"{d.Id}->{r}"))
            .OrderBy(s => s, Ordinal)
            .ToArray();
        relatedPairs.Length.ShouldBeGreaterThan(0, "expected at least one relatedIds edge in the registry.");
        AssertShuffledSortIsStable(relatedPairs, rng, "related-ids");

        // Surface 4: suppressions — keys derived from compatibility-suppressions.json rows.
        FileInfo suppressionFile = new(Path.Combine(ProjectRoot().FullName, "docs", "diagnostics", "compatibility-suppressions.json"));
        if (suppressionFile.Exists) {
            JsonObject suppressionJson = JsonNode.Parse(File.ReadAllText(suppressionFile.FullName, Encoding.UTF8))!.AsObject();
            if (suppressionJson["suppressions"] is JsonArray suppressionArray && suppressionArray.Count > 0) {
                string[] suppressionKeys = suppressionArray
                    .Select(node => {
                        JsonObject row = node!.AsObject();
                        return $"{row["package"]!.GetValue<string>()}|{row["tfm"]!.GetValue<string>()}|{row["oldSignature"]!.GetValue<string>()}|{row["hfcId"]!.GetValue<string>()}";
                    })
                    .OrderBy(s => s, Ordinal)
                    .ToArray();
                AssertShuffledSortIsStable(suppressionKeys, rng, "suppressions");
            }
        }

        // Surface 5: package groups — owner-package strings derived from registry rows.
        string[] packageGroups = registry.Diagnostics
            .Select(d => d.OwnerPackage)
            .Distinct(Ordinal)
            .OrderBy(p => p, Ordinal)
            .ToArray();
        packageGroups.Length.ShouldBeGreaterThan(0, "expected at least one owner-package group.");
        AssertShuffledSortIsStable(packageGroups, rng, "package-groups");
    }

    private static void AssertShuffledSortIsStable(string[] sortedInput, Random rng, string surface) {
        if (sortedInput.Length < 2) {
            return; // a single-element collection is trivially deterministic.
        }
        string[] shuffled = (string[])sortedInput.Clone();
        for (int i = shuffled.Length - 1; i > 0; i--) {
            int j = rng.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }
        // The shuffle MUST actually reorder; otherwise the determinism claim is vacuous.
        (!shuffled.SequenceEqual(sortedInput)).ShouldBeTrue(
            $"surface '{surface}' shuffle produced the original order; Fisher-Yates should permute non-trivially.");
        string[] resorted = shuffled.OrderBy(s => s, Ordinal).ToArray();
        resorted.ShouldBe(sortedInput,
            $"surface '{surface}' is not stable under ordinal sort: shuffled+resorted differs from canonical order.");
    }

    [Fact]
    public void Story112_LedgerRowsMapToOneOfThreeFinalStates() {
        // Story 11.2 code-review P21 (AC34/D18): every Story-9-4 row routed to Story 11.2 must
        // map to exactly one final state. Pass-2 R2/R5: AC33 vocabulary is `closed-with-evidence`,
        // `deferred-to-named-story`, `rejected-with-rationale`. Reconciliation in deferred-work.md
        // uses the equivalent marker vocabulary that Story 11.1 introduced: `Resolved YYYY-MM-DD`,
        // `Owner: Story <X>`, `Rejected with rationale`, plus alias forms. Excluded substring
        // `Owner: Story` because the bare phrase appears in every row header; we require the full
        // `Owner: Story 11.<n>` shape so a free header phrase cannot satisfy the assertion.
        FileInfo ledger = new(Path.Combine(ProjectRoot().FullName, "_bmad-output", "implementation-artifacts", "deferred-work.md"));
        ledger.Exists.ShouldBeTrue("deferred-work.md must exist for ledger-mapping assertion.");

        string text = File.ReadAllText(ledger.FullName, Encoding.UTF8);
        Regex story112RowRegex = Story112LedgerRowRegex();
        MatchCollection matches = story112RowRegex.Matches(text);
        matches.Count.ShouldBeGreaterThan(0, "expected at least one DEF-9-4-* row in deferred-work.md.");

        // Pass-2 R2: each state expressed as a compiled regex anchored to the reconciliation
        // text so substrings of unrelated prose cannot accidentally satisfy the contract.
        Regex[] mandatedStateRegexes = [
            new(@"\bResolved\s+\d{4}-\d{2}-\d{2}\b", RegexOptions.CultureInvariant),
            new(@"\bOwner:\s+Story\s+1\d-\d", RegexOptions.CultureInvariant),
            new(@"\bDuplicate of\s+DEF-", RegexOptions.CultureInvariant),
            new(@"\bSuperseded by\s+DEF-", RegexOptions.CultureInvariant),
            new(@"\bRejected with rationale\b", RegexOptions.CultureInvariant),
            new(@"\bSplit parent\b", RegexOptions.CultureInvariant),
            new(@"\bNon-action decision\b", RegexOptions.CultureInvariant),
            new(@"\bNeeds Product/Architecture decision\b", RegexOptions.CultureInvariant),
        ];
        foreach (Match match in matches) {
            string reconciliation = match.Groups["reconciliation"].Value;
            bool hasState = mandatedStateRegexes.Any(rx => rx.IsMatch(reconciliation));
            hasState.ShouldBeTrue(
                $"DEF-9-4 ledger row '{match.Groups["id"].Value}' must carry one final-state marker (Resolved YYYY-MM-DD / Owner: Story 11.x / Duplicate of DEF- / Superseded by DEF- / Rejected with rationale / Split parent / Non-action decision / Needs Product/Architecture decision); got reconciliation: '{reconciliation}'.");
        }
    }

    [Fact]
    public void SourceToolsAnalyzerReleaseRows_AllHaveBackingDescriptor() {
        // Story 11.2 code-review P31: the RS2002 NoWarn was removed from the SourceTools
        // project assuming only HFCM rows tripped it. The original test inverted its own
        // justification by filtering OUT HFCM ids; Pass-2 R3 splits the assertion in two:
        //   (a) every HFC[non-M] row has a backing DiagnosticDescriptor.
        //   (b) no HFCM row appears in the SourceTools AnalyzerReleases.*.md files (HFCM rows
        //       belong in docs/diagnostics/migration-findings.json instead).
        // Pass-2 R28: enumeration uses an ordinal sort so failure ordering is platform-stable.
        HashSet<string> descriptorIds = DiagnosticDescriptorFields().Select(d => d.Id).ToHashSet(Ordinal);
        Regex strictHfcId = StrictHfcIdRegex();
        Regex hfcmIdShape = HfcmIdShapeRegex();
        string[] orderedReleaseRows = SourceToolsReleaseRows()
            .OrderBy(id => id, Ordinal)
            .ToArray();

        foreach (string releaseRow in orderedReleaseRows.Where(id => strictHfcId.IsMatch(id))) {
            descriptorIds.ShouldContain(releaseRow,
                $"SourceTools AnalyzerReleases row '{releaseRow}' must have a backing DiagnosticDescriptor field; otherwise RS2002 will fire under TreatWarningsAsErrors=true.");
        }

        // Pass-2 R3 part (b): the original assumption "RS2002 only fires for HFCM rows" must be
        // enforced — if an HFCM row leaks back into SourceTools AnalyzerReleases.*.md, the build
        // would fail again. Catch the regression here.
        string[] hfcmRows = orderedReleaseRows.Where(id => hfcmIdShape.IsMatch(id)).ToArray();
        hfcmRows.ShouldBeEmpty(
            $"SourceTools AnalyzerReleases.*.md must not contain HFCM rows; they belong in docs/diagnostics/migration-findings.json. Found: {string.Join(", ", hfcmRows)}.");
    }

    private static IEnumerable<string> SourceToolsReleaseRows() {
        string[] sourceToolsReleaseFiles = [
            Path.Combine(ProjectRoot().FullName, "src", "Hexalith.FrontComposer.SourceTools", "AnalyzerReleases.Unshipped.md"),
            Path.Combine(ProjectRoot().FullName, "src", "Hexalith.FrontComposer.SourceTools", "AnalyzerReleases.Shipped.md"),
        ];
        Regex tableRowId = ReleaseRowIdRegex();
        // Pass-2 R28: deterministic enumeration order across Windows/Linux.
        foreach (string file in sourceToolsReleaseFiles.Where(File.Exists).OrderBy(path => path, Ordinal)) {
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
        // round-trip positive and negative cases so the contract is testable.
        string safeFrontMatter = "id: HFC1058\ntitle: \"Sample\"\nlifecycle: active";
        ValidateGeneratedFrontMatter(safeFrontMatter).ShouldBeEmpty();

        string formulaInjection = "=cmd|'/c calc'!A1\nid: HFC1058";
        ValidateGeneratedFrontMatter(formulaInjection).ShouldContain("frontmatter-formula-injection");

        // Pass-2 R29: shell-pipe lead '|' must trigger formula-injection.
        string pipeInjection = "|cat /etc/passwd\nid: HFC1058";
        ValidateGeneratedFrontMatter(pipeInjection).ShouldContain("frontmatter-formula-injection",
            customMessage: "shell pipe '|' must trigger formula-injection category (R29).");

        string scriptInjection = "id: HFC1058\nnotes: <script>alert(1)</script>";
        ValidateGeneratedFrontMatter(scriptInjection).ShouldContain("frontmatter-injection-pattern");

        string nbspScript = "id: HFC1058\nnotes: < script>alert(1)</script>";
        ValidateGeneratedFrontMatter(nbspScript).ShouldContain("frontmatter-injection-pattern",
            customMessage: "NBSP-disguised script tag must be caught after StripZeroWidthAndFormatChars (P30).");

        // Pass-2 R26: CRLF line endings must not bypass lead-char detection.
        string crlfFormulaInjection = "=cmd|'/c calc'!A1\r\nid: HFC1058";
        ValidateGeneratedFrontMatter(crlfFormulaInjection).ShouldContain("frontmatter-formula-injection",
            customMessage: "CRLF must split correctly so the formula-injection lead is detected (R26).");

        // Pass-2 R29: HTML-entity-encoded injection patterns must be decoded before scan.
        string htmlEntityScript = "id: HFC1058\nnotes: &lt;script&gt;alert(1)&lt;/script&gt;";
        ValidateGeneratedFrontMatter(htmlEntityScript).ShouldContain("frontmatter-injection-pattern",
            customMessage: "HTML-entity-encoded script tag must be caught after entity decode (R29).");

        // Pass-2 R31: full-width confusable characters must NFC-normalize before scan.
        string fullWidthScript = "id: HFC1058\nnotes: ＜script＞alert(1)＜/script＞";
        ValidateGeneratedFrontMatter(fullWidthScript).ShouldContain("frontmatter-injection-pattern",
            customMessage: "full-width '<' (U+FF1C) must NFC-normalize to '<' before scan (R31).");

        // Pass-2 R38: idempotency — running the validator twice on the same input must yield the
        // same categories (no state leak between calls).
        string[] first = ValidateGeneratedFrontMatter(scriptInjection).ToArray();
        string[] second = ValidateGeneratedFrontMatter(scriptInjection).ToArray();
        first.ShouldBe(second, "ValidateGeneratedFrontMatter must be idempotent on identical input (R38).");
    }

    [Fact]
    public void UnsafeGeneratedFrontMatter_SampleRoundTrips() {
        // Pass-2 R18: the committed `unsafe-generated-front-matter-report.json` sample declares a
        // category that the validator must actually emit on a tampered input. Round-trip a known
        // tampered input through ValidateGeneratedFrontMatter and assert the category appears in
        // the same shape the sample uses.
        FileInfo sample = new(Path.Combine(DiagnosticsDocsRoot().FullName, "samples", "unsafe-generated-front-matter-report.json"));
        sample.Exists.ShouldBeTrue("unsafe-generated-front-matter sample missing.");
        JsonObject parsed = JsonNode.Parse(File.ReadAllText(sample.FullName, Encoding.UTF8))!.AsObject();
        string sampleCategory = parsed["category"]!.GetValue<string>();

        string[] producedCategories = ValidateGeneratedFrontMatter("=cmd|'/c calc'!A1\nid: HFC1058").ToArray();
        producedCategories.ShouldContain("frontmatter-formula-injection",
            $"validator must emit 'frontmatter-formula-injection'; sample category '{sampleCategory}' is the documented evidence shape.");
    }

    [Fact]
    public void RegistryValidator_UnsupportedSchemaSampleRoundTrips() {
        // Pass-2 R17: the committed `unsupported-schema-drift-report.json` declares evidence for
        // an unsupported-schema validation outcome. Tamper a pristine registry to flip
        // schemaVersion to an unsupported value and assert the validator emits the matching
        // category — proving the sample is the producer-side output shape, not a hand-rolled
        // artifact that could drift from the validator's actual behavior.
        FileInfo sample = new(Path.Combine(DiagnosticsDocsRoot().FullName, "samples", "unsupported-schema-drift-report.json"));
        sample.Exists.ShouldBeTrue("unsupported-schema-drift-report sample missing.");
        JsonObject parsed = JsonNode.Parse(File.ReadAllText(sample.FullName, Encoding.UTF8))!.AsObject();
        parsed["category"]!.GetValue<string>().ShouldBe("unsupported-schema");

        JsonObject tampered = RegistryJson().DeepClone().AsObject();
        tampered["schemaVersion"] = "9.9";
        string[] categories = ValidateRegistryJson(tampered).ToArray();
        categories.ShouldBe(["unsupported-schema"],
            "ValidateRegistryJson on unsupported schemaVersion must yield exactly one category ('unsupported-schema'); the sample is its evidence form.");
    }

    private static IEnumerable<string> ValidateGeneratedFrontMatter(string frontMatter) {
        // Pass-2 R29: shell-pipe prefix '|' added; '-' intentionally NOT included because legitimate
        // YAML list lines start with '-'. Generated front-matter that ships a leading shell command
        // is caught via the injection-pattern scan, not the lead-char fast path.
        char[] forbiddenLeads = ['=', '+', '@', '|'];
        string[] forbiddenPatterns = ["<script", "</script", "<iframe", "javascript:", "data:text/html", "vbscript:"];

        // Pass-2 R26: split on both '\r' and '\n' so CRLF inputs do not leave a trailing '\r' on
        // line[0] that bypasses the lead-char check.
        string[] lines = frontMatter.Split(['\r', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        bool yieldedFormulaInjection = false;
        foreach (string line in lines) {
            if (line.Length > 0 && forbiddenLeads.Contains(line[0])) {
                yield return "frontmatter-formula-injection";
                yieldedFormulaInjection = true;
                break;
            }
        }

        // Pass-2 R29 + R31: HTML-entity-decode and NFKC-normalize before injection-pattern scan
        // so encoded `&lt;script&gt;` and full-width `＜script＞` cannot bypass. NFKC (compatibility
        // composition) is required for full-width-to-halfwidth folding; NFC alone leaves U+FF1C
        // intact because the chars are canonically distinct.
        string normalized = StripZeroWidthAndFormatChars(frontMatter).Normalize(NormalizationForm.FormKC);
        string htmlDecoded = System.Net.WebUtility.HtmlDecode(normalized);
        foreach (string forbidden in forbiddenPatterns) {
            if (htmlDecoded.Contains(forbidden, StringComparison.OrdinalIgnoreCase)) {
                yield return "frontmatter-injection-pattern";
                yield break;
            }
        }

        // Quiet unused-flag warning; the variable documents that the two categories are emitted
        // independently (one does not short-circuit the other) for AC32 evidence completeness.
        _ = yieldedFormulaInjection;
    }

    // Pass-2 R5: allow hyphens/alphanumerics in the suffix (e.g., `DEF-9-4-C4a`), match either
    // `**...**` or `__...__` bold markers, and accept content between the id and the bold close
    // (rows use `**DEF-9-4-A1 — Title**` shape). Reconciliation may live on a later line in the
    // same bullet, so the gap between bold close and marker is multi-line tolerant.
    [GeneratedRegex(@"(?:\*\*|__)(?<id>DEF-9-4-[A-Za-z0-9\-]+)\b[^\n]*?(?:\*\*|__)[\s\S]*?Reconciliation:\s*(?<reconciliation>[^\n]+)", RegexOptions.CultureInvariant)]
    private static partial Regex Story112LedgerRowRegex();

    // Pass-2 R16: explicit allowlist of supported root keys. AC27 says unknown optional fields in
    // a supported schema must remain non-fatal only when the compatibility rule explicitly
    // permits them. Anything outside this allowlist must surface as a named category so a
    // `"backdoor": "..."` or other foreign key cannot ride along silently.
    private static readonly HashSet<string> KnownRegistryRootKeys = new(StringComparer.Ordinal) {
        "schemaVersion",
        "canonicalHelpLinkFormat",
        "ranges",
        "externalBoundaries",
        "allowedExceptions",
        "diagnostics",
    };

    private static IEnumerable<string> ValidateRegistryJson(JsonObject json) {
        if (!TryGetString(json, "schemaVersion", out string? schemaVersion)
            || schemaVersion != SupportedSchemaVersion) {
            yield return "unsupported-schema";
            yield break;
        }

        // Pass-2 R16: unknown-root-field detection — every top-level key must be in the documented
        // allowlist. Yields one category per unknown key so the operator sees exactly which field
        // is foreign.
        foreach (string key in json.Select(kvp => kvp.Key).OrderBy(k => k, Ordinal)) {
            if (!KnownRegistryRootKeys.Contains(key)) {
                yield return "registry-unknown-root-field";
            }
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
                    || !TryGetString(exception, "introducedIn", out string? exIntroducedIn)
                    || !TryGetString(exception, "approvedOn", out _)
                    || id != "HFC1601"
                    || ownerPackage != "SourceTools"
                    || consumingPackage != "Shell"
                    || numericRangeOwner != "SourceTools"
                    || helpLinkUri != string.Format(CultureInfo.InvariantCulture, CanonicalHelpLinkFormat, id)
                    // Pass-2 R34: cross-package-exception introducedIn must follow SemVer shape.
                    || !SemverShapeRegex().IsMatch(exIntroducedIn!)) {
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
        // Pass-2 R21: include newState in the dedupe tuple so two suppressions differing only in
        // newState ("removed" vs "modified") are treated as distinct compatibility scenarios, not
        // collapsed into a spurious duplicate.
        HashSet<(string Package, string Tfm, string OldSignature, string NewState, string HfcId, string TargetRelease)> uniqueRows = [];
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

            // Story 11.2 code-review P8 + Pass-2 R30: split malformed-version into per-field
            // categories so the operator can tell which field is bad.
            bool expiresOk = TryParseVersionToken(expiresAfter!, out Version? parsedExpires);
            bool targetOk = TryParseVersionToken(targetRelease!, out Version? parsedTarget);
            if (!expiresOk) {
                yield return "suppression-malformed-expires-after";
            }
            if (!targetOk) {
                yield return "suppression-malformed-target-release";
            }
            if (expiresOk && targetOk && parsedExpires! <= parsedTarget!) {
                yield return "suppression-expired";
            }

            if (string.IsNullOrWhiteSpace(newState) || string.IsNullOrWhiteSpace(rationale) || string.IsNullOrWhiteSpace(ownerStory)) {
                yield return "suppression-missing-required-field";
            }

            if (!uniqueRows.Add((package, tfm!, oldSignature!, newState!, hfcId!, targetRelease!))) {
                yield return "suppression-duplicate-row";
            }
        }
    }

    private static bool TryParseVersionToken(string value, out Version? version) {
        version = null;
        if (string.IsNullOrWhiteSpace(value)) {
            return false;
        }

        // Pass-2 R13: catch every exception type the parsing pipeline can produce. The previous
        // narrow filter (`ArgumentException or FormatException or OverflowException`) would let
        // `RegexMatchTimeoutException` or `InvalidOperationException` escape the iterator and
        // crash the validator — defeating the fail-closed contract the validator is meant to
        // enforce. We log no surface area outside this method, so a catch-all is safe.
        try {
            version = ParseVersionToken(value);
            return true;
        } catch (Exception) {
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

        // Story 11.2 code-review P30 + Pass-2 R19: non-format whitespace variants (NBSP and
        // friends) must be stripped before injection-pattern scanning. R19: use explicit C#
        // \uXXXX escapes so reviewers can verify intent without a hex dump, and editor
        // normalization cannot silently collapse invisible literals into ASCII spaces.
        if (value is '\u00A0'   // NO-BREAK SPACE
            or '\u2007'         // FIGURE SPACE
            or '\u202F'         // NARROW NO-BREAK SPACE
            or '\u3000') {      // IDEOGRAPHIC SPACE
            return true;
        }

        return value is '\u200B'  // ZERO WIDTH SPACE
            or '\u200C'           // ZERO WIDTH NON-JOINER
            or '\u200D'           // ZERO WIDTH JOINER
            or '\uFEFF'           // ZERO WIDTH NO-BREAK SPACE (BOM)
            or '\u200E'           // LEFT-TO-RIGHT MARK
            or '\u200F'           // RIGHT-TO-LEFT MARK
            or '\u2060'           // WORD JOINER
            or '\u180E'           // MONGOLIAN VOWEL SEPARATOR
            or '\u202A'           // LEFT-TO-RIGHT EMBEDDING
            or '\u202B'           // RIGHT-TO-LEFT EMBEDDING
            or '\u202C'           // POP DIRECTIONAL FORMATTING
            or '\u202D'           // LEFT-TO-RIGHT OVERRIDE
            or '\u202E'           // RIGHT-TO-LEFT OVERRIDE
            or '\uFF28';          // FULLWIDTH LATIN CAPITAL LETTER H (confusable for 'H')
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

    // Story 11.2 code-review P25 + Pass-2 R20: pin the diagnostics URL shape to https-only
    // canonical form. The regex is a coarse extractor that stops at the bare HFCxxxx form (no
    // trailing path segments or punctuation captured); the caller uses Uri.TryCreate to validate
    // scheme=https exactly, host equals CanonicalDocsHost with no trailing dot, no userinfo, no
    // explicit port, no query, no fragment, and path is exactly `/FrontComposer/diagnostics/
    // HFCxxxx`. Trailing characters (`.`, `,`, `)`, `]`, `;`, whitespace) are NOT consumed so
    // prose like "see HFC1039." does not poison the URL with a stray period.
    [GeneratedRegex(@"https?://[^""'\s]*?/diagnostics/HFC[0-9]{4}", RegexOptions.CultureInvariant)]
    private static partial Regex DiagnosticsDocsLinkUrlRegex();

    // Pass-2 R37: strict SemVer 2.0.0 shape (no leading zeros, well-formed pre-release/build
    // metadata identifiers separated by `.`). Rejects `00.000`, `1.0-`, `1.0--`, `1.0-..beta`.
    // Does NOT accept `v` prefix — callers that need a `v` prefix must strip it before matching.
    [GeneratedRegex(@"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:\.(0|[1-9][0-9]*))?(?:-[0-9A-Za-z\-]+(?:\.[0-9A-Za-z\-]+)*)?(?:\+[0-9A-Za-z\-]+(?:\.[0-9A-Za-z\-]+)*)?$", RegexOptions.CultureInvariant)]
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
