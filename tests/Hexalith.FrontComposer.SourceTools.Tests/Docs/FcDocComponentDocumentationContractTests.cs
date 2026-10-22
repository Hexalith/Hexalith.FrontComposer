using System.Text.RegularExpressions;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Docs;

/// <summary>
/// Story 1.5 (FC-DOC) ready-gate pin tests. The PowerShell validator (Gate 2d) is the runtime gate;
/// the generic <see cref="DocsSiteValidationTests"/> pins front-matter, uid/slug uniqueness, and the
/// four top-level Diataxis entries across <c>reference/**</c>. Neither pins the <b>FC-DOC-specific</b>
/// contract — the required section set, the <c>csharp</c>-fence marker rule, the published
/// <c>HFC1050</c>–<c>HFC1055</c> accessibility cross-links, and the component status-map coverage.
/// These tests keep that contract visible in the normal .NET suite so a malformed component page is
/// caught without shelling out to <c>eng/validate-docs.ps1</c>.
/// </summary>
/// <remarks>
/// Source contract: <c>_bmad-output/contracts/fc-doc-component-documentation-2026-06-03.md</c>.
/// </remarks>
[Trait("Category", "Governance")]
public sealed partial class FcDocComponentDocumentationContractTests {
    /// <summary>The canonical section set every published component reference page MUST contain.</summary>
    private static readonly string[] RequiredSections =
    [
        "## Overview",
        "## Usage",
        "## Parameters / slots",
        "## Layout (FC-LYT)",
        "## Accessibility (FC-A11Y)",
        "## Localization (FC-L10N)",
        "## Related",
    ];

    /// <summary>The six published accessibility-override diagnostic pages the a11y section links.</summary>
    private static readonly string[] AccessibilityDiagnostics =
        ["HFC1050", "HFC1051", "HFC1052", "HFC1053", "HFC1054", "HFC1055"];

    public static TheoryData<string> ComponentPages() {
        TheoryData<string> data = [];
        foreach (string file in ComponentPageFiles()) {
            data.Add(Relative(file));
        }

        return data;
    }

    [Fact]
    public void ComponentsAreaHasAtLeastTheAnchorAndOneMorePage() {
        string[] pages = ComponentPageFiles().Select(Relative).ToArray();

        pages.ShouldContain(
            "docs/reference/components/front-composer-shell.md",
            "The FC-DOC anchor page (FrontComposerShell) must be authored to prove the contract.");
        pages.Length.ShouldBeGreaterThanOrEqualTo(
            2,
            "FC-DOC requires the anchor plus at least one more conforming page (Navigation) to prove repeatability.");
    }

    [Theory]
    [MemberData(nameof(ComponentPages))]
    public void EveryComponentPageContainsAllRequiredSections(string relativePath) {
        ArgumentNullException.ThrowIfNull(relativePath);

        string page = File.ReadAllText(Absolute(relativePath));

        foreach (string section in RequiredSections) {
            page.ShouldContain(
                section,
                customMessage: $"{relativePath} must contain the FC-DOC required section '{section}'.");
        }
    }

    [Theory]
    [MemberData(nameof(ComponentPages))]
    public void EveryComponentPageUsesReferenceGenreAndAdopterAudience(string relativePath) {
        ArgumentNullException.ThrowIfNull(relativePath);

        Dictionary<string, string> frontMatter = ReadFrontMatter(Absolute(relativePath));

        frontMatter.GetValueOrDefault("genre").ShouldBe(
            "reference",
            $"{relativePath} must declare genre: reference.");
        frontMatter.GetValueOrDefault("audience").ShouldBe(
            "adopter",
            $"{relativePath} must declare audience: adopter.");
    }

    [Theory]
    [MemberData(nameof(ComponentPages))]
    public void EveryCSharpFenceIsMarkedCompileOrNoCompileWithReason(string relativePath) {
        ArgumentNullException.ThrowIfNull(relativePath);

        string page = File.ReadAllText(Absolute(relativePath));

        foreach (Match fence in CSharpFenceOpener().Matches(page)) {
            string info = fence.Groups[1].Value.Trim();

            bool isBareCompile = info == "compile";
            bool isNoCompileWithReason =
                info.StartsWith("no-compile", StringComparison.Ordinal)
                && NoCompileReason().IsMatch(info);

            (isBareCompile || isNoCompileWithReason).ShouldBeTrue(
                $"{relativePath}: a ```csharp fence opened with '```csharp {info}' must be marked " +
                "`compile` or `no-compile reason=\"…\"` (Gate-2d snippet rule). Adopter usage should " +
                "use a ```razor fence instead.");
        }
    }

    [Theory]
    [MemberData(nameof(ComponentPages))]
    public void EveryComponentPageIsFreeOfUnsafeText(string relativePath) {
        ArgumentNullException.ThrowIfNull(relativePath);

        string page = File.ReadAllText(Absolute(relativePath));

        foreach (string privatePath in new[] { "/home/", "C:\\Users\\", "/Users/" }) {
            page.Contains(privatePath, StringComparison.OrdinalIgnoreCase).ShouldBeFalse(
                $"{relativePath} must not embed an absolute private path ('{privatePath}').");
        }

        page.IndexOf('\u001b').ShouldBe(-1, $"{relativePath} must not contain terminal control sequences.");
        TenantOrSecretLiteral().IsMatch(page).ShouldBeFalse(
            $"{relativePath} must not embed a tenant-id/secret/api-key/password literal.");
    }

    [Fact]
    public void AnchorPageLinksAllPublishedAccessibilityDiagnostics() {
        string anchor = File.ReadAllText(Absolute("docs/reference/components/front-composer-shell.md"));

        foreach (string id in AccessibilityDiagnostics) {
            anchor.ShouldContain(
                $"diagnostics/{id}.md",
                customMessage: $"The FrontComposerShell Accessibility section must link the published {id} diagnostic page.");

            File.Exists(Absolute($"docs/diagnostics/{id}.md")).ShouldBeTrue(
                $"{id} must be a published diagnostic page for the anchor page's link to resolve.");
        }
    }

    [Theory]
    [MemberData(nameof(ComponentPages))]
    public void EveryComponentPageLinksAtLeastOnePublishedAccessibilityDiagnostic(string relativePath) {
        ArgumentNullException.ThrowIfNull(relativePath);

        string page = File.ReadAllText(Absolute(relativePath));

        bool linksADiagnostic = AccessibilityDiagnostics.Any(id => page.Contains($"diagnostics/{id}.md", StringComparison.Ordinal));

        linksADiagnostic.ShouldBeTrue(
            $"{relativePath}'s Accessibility (FC-A11Y) section must link at least one published " +
            "HFC1050–HFC1055 diagnostic page (the FC-DOC cross-link convention).");
    }

    [Fact]
    public void ComponentsIndexListsEveryAuthoredComponentPage() {
        string index = File.ReadAllText(Absolute("docs/reference/components/index.md"));

        foreach (string file in ComponentPageFiles()) {
            string fileName = Path.GetFileName(file);
            index.ShouldContain(
                $"({fileName})",
                customMessage: $"reference/components/index.md must link the authored page '{fileName}'.");
        }
    }

    [Fact]
    public void TocNestsComponentsUnderReferenceWithoutAddingATopLevelEntry() {
        string[] lines = File.ReadAllLines(Absolute("docs/toc.yml"));

        // Top-level entries are the unindented `- name:` lines; FC-DOC must not add a fifth.
        string[] topLevel = lines
            .Select(line => Regex.Match(line, "^- name:\\s*(.+?)\\s*$"))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value)
            .ToArray();

        topLevel.ShouldBe(
            ["Tutorials", "How-to", "Reference", "Concepts"],
            "FC-DOC must keep exactly four top-level Diataxis entries — Components is a nested item, never a fifth top-level entry.");

        // Components must appear as an indented item pointing at the components index, under Reference.
        int referenceIndex = Array.FindIndex(lines, line => line.StartsWith("- name: Reference", StringComparison.Ordinal));
        referenceIndex.ShouldBeGreaterThanOrEqualTo(0, "toc.yml must contain a top-level Reference node.");

        int concptsIndex = Array.FindIndex(lines, line => line.StartsWith("- name: Concepts", StringComparison.Ordinal));
        int searchEnd = concptsIndex < 0 ? lines.Length : concptsIndex;

        bool componentsNestedUnderReference = lines
            .Skip(referenceIndex + 1)
            .Take(searchEnd - referenceIndex - 1)
            .Any(line => line.TrimStart().StartsWith("- name: Components", StringComparison.Ordinal)
                && line.StartsWith(' '));

        componentsNestedUnderReference.ShouldBeTrue(
            "A `Components` group must be nested under the Reference node in docs/toc.yml.");

        lines.Any(line => Regex.IsMatch(line, @"^[ \t]*(?:-[ \t]+)?href:[ \t]+reference/components/index\.md[ \t]*(?:#.*)?$")).ShouldBeTrue(
            "The Components TOC group must point at reference/components/index.md.");
    }

    [Fact]
    public void ComponentStatusMapCoversTheReadOnlyMvpSetAsAPageOrTrackedGapWithOwner() {
        string contract = File.ReadAllText(Absolute("_bmad-output/contracts/fc-doc-component-documentation-2026-06-03.md"));

        // The read-only-MVP set per AC2 — every area must resolve to an authored page OR a tracked gap with an owner.
        foreach (string area in new[] { "Layout & frame", "Navigation", "DataGrid surface", "Settings" }) {
            string row = ContractTableRow(contract, area);
            row.ShouldNotBeNullOrWhiteSpace($"FC-DOC component status map must include a row for '{area}'.");

            string[] cells = row.Split('|', StringSplitOptions.TrimEntries);

            // Row shape: | area | anchor component(s) | doc page | status | owner |
            string docPage = cells.Length > 3 ? cells[3] : string.Empty;
            string status = cells.Length > 4 ? cells[4] : string.Empty;
            string owner = cells.Length > 5 ? cells[5] : string.Empty;

            owner.ShouldNotBeNullOrWhiteSpace($"'{area}' must record a named owner in the status map.");

            bool isAuthored = status.Contains("authored", StringComparison.OrdinalIgnoreCase);
            bool isTrackedGap = status.Contains("tracked gap", StringComparison.OrdinalIgnoreCase);

            (isAuthored || isTrackedGap).ShouldBeTrue(
                $"'{area}' must be either authored or a tracked gap (AC2). Status was: '{status}'.");

            if (isAuthored) {
                Match pagePath = Regex.Match(docPage, "`(docs/reference/components/[^`]+\\.md)`");
                pagePath.Success.ShouldBeTrue($"'{area}' is marked authored but its status-map row names no doc page.");
                File.Exists(Absolute(pagePath.Groups[1].Value)).ShouldBeTrue(
                    $"'{area}' is marked authored but {pagePath.Groups[1].Value} does not exist.");
            }
        }
    }

    [Fact]
    public void CurrentInventoryAndPublishedContractsMatchSource()
    {
        ValidateCurrentContracts(ReadContractFiles()).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("missing-page", "docs/reference/components/page-patterns.md")]
    [InlineData("unindexed", "docs/reference/components/index.md")]
    [InlineData("commented-index-link", "docs/reference/components/index.md")]
    [InlineData("unterminated-comment-index-link", "docs/reference/components/index.md")]
    [InlineData("commented-toc-href", "docs/toc.yml")]
    [InlineData("added-qualified-parameter", "docs/reference/components/page-toolbar.md")]
    [InlineData("added-global-qualified-parameter", "docs/reference/components/page-toolbar.md")]
    [InlineData("added-attribute-suffixed-parameter", "docs/reference/components/page-toolbar.md")]
    [InlineData("unclassified", "_bmad-output/project-docs/component-inventory.md")]
    [InlineData("stale-parameter", "docs/reference/components/page-toolbar.md")]
    [InlineData("removed-toolbar-search-callback", "docs/reference/components/page-toolbar.md")]
    [InlineData("removed-toolbar-tab-callback", "docs/reference/components/page-toolbar.md")]
    [InlineData("removed-page-tab-callback", "docs/reference/components/page-tabs.md")]
    [InlineData("stale-state", "docs/reference/components/page-patterns.md")]
    [InlineData("stale-type", "docs/reference/components/page-tabs.md")]
    [InlineData("swapped-grouped-types", "docs/reference/components/page-toolbar.md")]
    [InlineData("added-init-property", "docs/reference/components/customization.md")]
    [InlineData("added-set-property", "docs/reference/components/customization.md")]
    [InlineData("added-expression-state", "docs/reference/components/page-patterns.md")]
    [InlineData("stale-route-focus", "docs/reference/components/front-composer-shell.md")]
    [InlineData("stale-route-body", "docs/reference/components/front-composer-shell.md")]
    [InlineData("stale-context", "docs/reference/components/customization.md")]
    [InlineData("stale-infrastructure", "docs/reference/components/customization.md")]
    [InlineData("missing-view-context", "docs/reference/components/customization.md")]
    [InlineData("missing-template-context", "docs/reference/components/customization.md")]
    [InlineData("missing-slot-context", "docs/reference/components/customization.md")]
    [InlineData("missing-detail-states", "docs/reference/components/page-patterns.md")]
    [InlineData("missing-command-states", "docs/reference/components/command-lifecycle.md")]
    [InlineData("missing-connection-states", "docs/reference/components/projection-status.md")]
    [InlineData("missing-badge-states", "docs/reference/components/projection-status.md")]
    public void CurrentContractRejectsDeterministicNegativeFixtures(string defect, string path)
    {
        ArgumentNullException.ThrowIfNull(defect);
        Dictionary<string, string> files = ReadContractFiles();
        files[path] = defect switch
        {
            "missing-page" => string.Empty,
            "removed-toolbar-search-callback" or "removed-toolbar-tab-callback" or "removed-page-tab-callback"
                or "added-init-property" or "added-set-property" or "added-expression-state"
                or "added-qualified-parameter" or "added-global-qualified-parameter"
                or "added-attribute-suffixed-parameter" => files[path],
            "missing-view-context" => RemoveSection(files[path], "### `ProjectionViewContext`"),
            "missing-template-context" => RemoveSection(files[path], "### `ProjectionTemplateContext`"),
            "missing-slot-context" => RemoveSection(files[path], "### `FieldSlotContext`"),
            "missing-detail-states" => RemoveSection(files[path], "### `FcAggregateDetailState`"),
            "missing-command-states" => RemoveSection(files[path], "### `CommandLifecycleState`"),
            "missing-connection-states" => RemoveSection(files[path], "### `ProjectionConnectionStatus`"),
            "missing-badge-states" => RemoveSection(files[path], "### `OptimisticBadgeState`"),
            "unindexed" => files[path].Replace("(page-patterns.md)", "(absent.md)", StringComparison.Ordinal),
            "commented-index-link" => files[path].Replace("[Page patterns](page-patterns.md)",
                "<!--\n[Page patterns](page-patterns.md)\n-->", StringComparison.Ordinal),
            "unterminated-comment-index-link" => files[path].Replace("[Page patterns](page-patterns.md)",
                "<!--\n[Page patterns](page-patterns.md)", StringComparison.Ordinal),
            "commented-toc-href" => files[path].Replace("href: reference/components/page-patterns.md",
                "# href: reference/components/page-patterns.md", StringComparison.Ordinal),
            "unclassified" => files[path].Replace("| `FcPageToolbar` | adopter |", "| `FcPageToolbar` | unknown |", StringComparison.Ordinal),
            "stale-parameter" => files[path].Replace("`SearchDisabled`", "`RemovedSearchParameter`", StringComparison.Ordinal),
            "stale-state" => files[path].Replace("| `Ready` |", "| `RemovedReadyState` |", StringComparison.Ordinal),
            "stale-infrastructure" => files[path].Replace("`CanRetry`", "`RemovedRetry`", StringComparison.Ordinal),
            "stale-context" => files[path].Replace("`DefaultBody`", "`RemovedDefaultBody`", StringComparison.Ordinal),
            "swapped-grouped-types" => files[path].Replace("| `SearchValue` / `SearchValueChanged` | `string?` / `EventCallback<string?>` |",
                "| `SearchValue` / `SearchValueChanged` | `EventCallback<string?>` / `string?` |", StringComparison.Ordinal),
            "stale-route-focus" => files[path].Replace("`FocusHeading`", "`RemovedFocusHeading`", StringComparison.Ordinal),
            "stale-route-body" => files[path].Replace("| `Body` | `RenderFragment?` |", "| `RemovedBody` | `RenderFragment?` |", StringComparison.Ordinal),
            "stale-type" => files[path].Replace("| `ModuleRoute` | `string?` |", "| `ModuleRoute` | `bool` |", StringComparison.Ordinal),
            _ => throw new ArgumentException("Unknown synthetic defect.", nameof(defect)),
        };

        if (defect.StartsWith("removed-", StringComparison.Ordinal))
        {
            string component = defect == "removed-page-tab-callback" ? "FcPageTabs" : "FcPageToolbar";
            string parameter = defect == "removed-toolbar-search-callback" ? "SearchValueChanged" : "ActiveTabIdChanged";
            string sourcePath = $"src/Hexalith.FrontComposer.Shell/Components/Layout/{component}.razor.cs";
            string original = files[sourcePath];
            files[sourcePath] = Regex.Replace(original,
                $@"\[Parameter\]\s+public EventCallback<string\?> {parameter} \{{ get; set; \}}", string.Empty);
            files[sourcePath].ShouldNotBe(original, "The fixture must remove an existing callback parameter.");
        }
        if (defect is "added-qualified-parameter" or "added-global-qualified-parameter" or "added-attribute-suffixed-parameter")
        {
            const string sourcePath = "src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageToolbar.razor.cs";
            string attribute = defect switch
            {
                "added-global-qualified-parameter" => "global::Microsoft.AspNetCore.Components.Parameter",
                "added-attribute-suffixed-parameter" => "ParameterAttribute",
                _ => "Microsoft.AspNetCore.Components.Parameter",
            };
            files[sourcePath] = files[sourcePath].Insert(files[sourcePath].LastIndexOf('}'),
                $"    [{attribute}]\n    public string? AddedParameter {{ get; set; }}\n");
        }
        if (defect is "added-init-property" or "added-set-property")
        {
            const string sourcePath = "src/Hexalith.FrontComposer.Contracts.UI/Rendering/ProjectionViewContext.cs";
            string accessor = defect == "added-init-property" ? "init" : "set";
            files[sourcePath] = files[sourcePath].Insert(files[sourcePath].LastIndexOf('}'),
                $"    public string? AddedContextMember {{ get; {accessor}; }}\n");
        }
        if (defect == "added-expression-state")
        {
            const string sourcePath = "src/Hexalith.FrontComposer.Contracts/Rendering/FcAggregateDetailState.cs";
            files[sourcePath] = files[sourcePath].Insert(files[sourcePath].LastIndexOf('}'),
                "    AddedEnumMember = 1 << 20,\n");
        }
        string[] failures = ValidateCurrentContracts(files);
        failures.ShouldNotBeEmpty();
        failures.ShouldContain(failure => failure.StartsWith(path + ":", StringComparison.Ordinal));
        if (defect.StartsWith("removed-", StringComparison.Ordinal))
        {
            string component = defect == "removed-page-tab-callback" ? "FcPageTabs" : "FcPageToolbar";
            string parameter = defect == "removed-toolbar-search-callback" ? "SearchValueChanged" : "ActiveTabIdChanged";
            failures.ShouldContain($"{path}: {component} has stale parameter {parameter}.");
        }
        ValidateCurrentContracts(files).ShouldBe(failures);
        string.Join("\n", failures).ShouldNotContain(ProjectRoot());
    }

    [Theory]
    [InlineData("ComponentBase")]
    [InlineData("IComponent")]
    [InlineData("CustomBase")]
    [InlineData("PrimaryConstructor")]
    [InlineData("SplitPartial")]
    public void CurrentContractUnclassifiedOrdinaryComponentFails(string baseType)
    {
        Dictionary<string, string> files = ReadContractFiles();
        files["src/Hexalith.FrontComposer.Shell/Components/Synthetic/PlainWidget.cs"] =
            $"public sealed class PlainWidget : {baseType} {{ }}";
        if (baseType == "CustomBase")
        {
            files["src/Hexalith.FrontComposer.Shell/Components/Synthetic/CustomBase.cs"] =
                "public abstract class CustomBase : Microsoft.AspNetCore.Components.ComponentBase { }";
        }
        if (baseType == "PrimaryConstructor")
        {
            files["src/Hexalith.FrontComposer.Shell/Components/Synthetic/PlainWidget.cs"] =
                "public sealed class PlainWidget() : ComponentBase { }";
        }
        if (baseType == "SplitPartial")
        {
            files["src/Hexalith.FrontComposer.Shell/Components/Synthetic/PlainWidget.cs"] =
                "public sealed partial class PlainWidget : ComponentBase { }";
            files["src/Hexalith.FrontComposer.Shell/Components/Synthetic/PlainWidget.Part.cs"] =
                "public partial class PlainWidget { }";
        }
        ValidateCurrentContracts(files).ShouldContain(
            "_bmad-output/project-docs/component-inventory.md: unclassified component PlainWidget.");
    }

    private static string RemoveSection(string text, string heading)
    {
        string section = Section(text, heading);
        section.ShouldNotBeEmpty("The negative fixture must delete an existing complete section.");
        return text.Replace(heading + section, string.Empty, StringComparison.Ordinal);
    }

    private static string[] DiscoverComponents(Dictionary<string, string> files)
    {
        HashSet<string> componentTypes = new(["ComponentBase", "IComponent", "FluxorComponent", "LayoutComponentBase", "ErrorBoundaryBase", "ErrorBoundary"], StringComparer.Ordinal);
        HashSet<string> components = files.Keys.Where(path => path.StartsWith("src/Hexalith.FrontComposer.Shell/Components/", StringComparison.Ordinal)
            && path.EndsWith(".razor", StringComparison.Ordinal)).Select(path => Path.GetFileNameWithoutExtension(path)).ToHashSet(StringComparer.Ordinal);
        componentTypes.UnionWith(components);
        Dictionary<string, (bool IsClass, string[] Bases)> declarations = new(StringComparer.Ordinal);
        foreach (var source in files.Where(file => file.Key.StartsWith("src/Hexalith.FrontComposer.Shell/Components/", StringComparison.Ordinal) && file.Key.EndsWith(".cs", StringComparison.Ordinal)))
        {
            foreach (Match declaration in Regex.Matches(source.Value, @"(?m)^\s*(?:(?:public|internal|private|protected|abstract|sealed|partial|static|new)\s+)*(?<kind>class|interface)\s+(?<name>\w+)(?:<[^>{}]+>)?\s*(?:\([^{};]*\))?\s*(?::\s*(?<bases>[^{;]+))?[{;]"))
            {
                string bases = Regex.Replace(declaration.Groups["bases"].Value, @"\bwhere\b.*", string.Empty, RegexOptions.Singleline);
                while (Regex.IsMatch(bases, @"<[^<>]*>"))
                {
                    bases = Regex.Replace(bases, @"<[^<>]*>", string.Empty);
                }
                string name = declaration.Groups["name"].Value;
                string[] inheritedTypes = bases.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(baseName => baseName.Split('.').Last()).ToArray();
                if (declarations.TryGetValue(name, out var previous))
                {
                    inheritedTypes = previous.Bases.Union(inheritedTypes, StringComparer.Ordinal).ToArray();
                }
                declarations[name] = (declaration.Groups["kind"].Value == "class", inheritedTypes);
            }
        }
        bool changed;
        do
        {
            changed = false;
            foreach (var declaration in declarations)
            {
                if (declaration.Value.Bases.Any(componentTypes.Contains) && componentTypes.Add(declaration.Key))
                {
                    changed = true;
                }
                if (declaration.Value.IsClass && componentTypes.Contains(declaration.Key))
                {
                    components.Add(declaration.Key);
                }
            }
        } while (changed);
        return components.Order(StringComparer.Ordinal).ToArray();
    }

    private static Dictionary<string, string> ReadContractFiles()
        => Directory.EnumerateFiles(Absolute("docs/reference/components"), "*.md")
            .Concat(Directory.EnumerateFiles(Absolute("src/Hexalith.FrontComposer.Shell/Components"), "*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".cs", StringComparison.Ordinal) || path.EndsWith(".razor", StringComparison.Ordinal)))
            .Concat(Directory.EnumerateFiles(Absolute("src/Hexalith.FrontComposer.Contracts"), "*.cs", SearchOption.AllDirectories))
            .Concat(Directory.EnumerateFiles(Absolute("src/Hexalith.FrontComposer.Contracts.UI/Rendering"), "*Context.cs"))
            .Concat([Absolute("src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionStatus.cs"),
                Absolute("_bmad-output/project-docs/component-inventory.md"),
                Absolute("_bmad-output/contracts/fc-doc-component-documentation-2026-06-03.md"), Absolute("docs/toc.yml")])
            .ToDictionary(Relative, File.ReadAllText, StringComparer.Ordinal);

    private static string[] ValidateCurrentContracts(Dictionary<string, string> files)
    {
        const string inventoryPath = "_bmad-output/project-docs/component-inventory.md";
        const string statusPath = "_bmad-output/contracts/fc-doc-component-documentation-2026-06-03.md";
        const string indexPath = "docs/reference/components/index.md";
        List<string> failures = [];
        string index = Regex.Replace(files[indexPath], @"<!--.*?(?:-->|\z)", string.Empty, RegexOptions.Singleline);
        string inventory = files[inventoryPath].Split("## H. Current component documentation classifications", StringSplitOptions.None).Last();
        Dictionary<string, (string Classification, string Page)> rows = new(StringComparer.Ordinal);
        foreach (Match row in Regex.Matches(inventory, @"(?m)^\| `(?<name>\w+)` \| (?<classification>[^|]+) \| `(?<page>docs/reference/components/[^`]+)` \|"))
        {
            rows[row.Groups["name"].Value] = (row.Groups["classification"].Value.Trim(), row.Groups["page"].Value);
        }

        string[] componentNames = DiscoverComponents(files);
        Dictionary<string, string> contracts = new(StringComparer.Ordinal);
        foreach (var source in files.Where(file => file.Key.StartsWith("src/", StringComparison.Ordinal)))
        {
            foreach (Match contract in Regex.Matches(source.Value, @"public (?:sealed )?class (?<name>\w+Context)(?:<|\s)|public enum (?<name>\w+State|ProjectionConnectionStatus)\b"))
            {
                contracts[contract.Groups["name"].Value] = source.Value;
            }
        }
        Dictionary<string, HashSet<string>> requiredContracts = new(StringComparer.Ordinal);
        foreach (string name in componentNames)
        {
            if (!rows.TryGetValue(name, out var row) || row.Classification is not ("adopter" or "infrastructure"))
            {
                failures.Add($"{inventoryPath}: unclassified component {name}.");
                continue;
            }
            if (!files.TryGetValue(row.Page, out string? page) || string.IsNullOrWhiteSpace(page))
            {
                failures.Add($"{row.Page}: missing classified component page.");
                continue;
            }
            string sourceText = string.Join("\n", files.Where(file => file.Key.StartsWith("src/Hexalith.FrontComposer.Shell/Components/", StringComparison.Ordinal)
                && Path.GetFileName(file.Key).Split('.')[0] == name).Select(file => file.Value));
            if (!requiredContracts.TryGetValue(row.Page, out HashSet<string>? required))
            {
                required = new(StringComparer.Ordinal);
                requiredContracts[row.Page] = required;
            }
            required.UnionWith(contracts.Keys.Where(contract => Regex.IsMatch(sourceText, $@"\b{Regex.Escape(contract)}\b")));
            string parameterSection = Section(page, "## Parameters / slots");
            string typedSection = Section(parameterSection, $"### `{name}`");
            if (row.Classification == "infrastructure" && string.IsNullOrEmpty(typedSection))
            {
                continue;
            }
            string documented = string.IsNullOrEmpty(typedSection) ? parameterSection : typedSection;
            Dictionary<string, string> actual = new(StringComparer.Ordinal);
            if (Regex.IsMatch(sourceText, @"(?m)^@inherits\s+(?:Microsoft\.AspNetCore\.Components\.)?LayoutComponentBase\b"))
            {
                actual["Body"] = "RenderFragment?";
            }
            foreach (Match parameter in Regex.Matches(sourceText, @"\[(?:global::)?(?:Microsoft\.AspNetCore\.Components\.)?Parameter(?:Attribute)?\b[^\]]*\](?:\s*\[[^\]]*\])*\s*public\s+(?<type>[^\r\n{]+?)\s+(?<name>\w+)\s*\{"))
            {
                actual[parameter.Groups["name"].Value] = parameter.Groups["type"].Value.Trim();
            }
            foreach (var parameter in actual)
            {
                string? docRow = documented.Split('\n').FirstOrDefault(line => line.StartsWith('|')
                    && line.Split('|')[1].Contains($"`{parameter.Key}`", StringComparison.Ordinal));
                string[] documentedNames = docRow is null ? [] : Regex.Matches(docRow.Split('|')[1], @"`(\w+)`")
                    .Select(match => match.Groups[1].Value).ToArray();
                string[] documentedTypes = docRow is null ? [] : Regex.Matches(docRow.Split('|')[2], @"`([^`]+)`")
                    .Select(match => match.Groups[1].Value).ToArray();
                int parameterIndex = Array.IndexOf(documentedNames, parameter.Key);
                if (parameterIndex < 0 || documentedNames.Length != documentedTypes.Length
                    || !string.Equals(documentedTypes[parameterIndex], parameter.Value, StringComparison.Ordinal))
                {
                    failures.Add($"{row.Page}: {name}.{parameter.Key} missing or stale parameter/type.");
                }
            }
            foreach (Match parameter in Regex.Matches(documented, @"(?m)^\| (?<names>[^|]+) \| (?<types>[^|]+) \|"))
            {
                foreach (Match identifier in Regex.Matches(parameter.Groups["names"].Value, @"`(\w+)`"))
                {
                    if (!actual.ContainsKey(identifier.Groups[1].Value))
                    {
                        failures.Add($"{row.Page}: {name} has stale parameter {identifier.Groups[1].Value}.");
                    }
                }
            }
        }
        foreach (string stale in rows.Keys.Except(componentNames, StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            failures.Add($"{inventoryPath}: stale component classification {stale}.");
        }
        foreach (var page in files.Where(file => file.Key.StartsWith("docs/reference/components/", StringComparison.Ordinal)
            && !file.Key.EndsWith("/index.md", StringComparison.Ordinal)).OrderBy(file => file.Key, StringComparer.Ordinal))
            {
            string fileName = Path.GetFileName(page.Key);
            if (!Regex.IsMatch(index, $@"\[[^\]\r\n]+\]\({Regex.Escape(fileName)}\)"))
            {
                failures.Add($"{indexPath}: missing page {fileName}.");
            }
            if (!Regex.IsMatch(files["docs/toc.yml"], $@"(?m)^[ \t]*(?:-[ \t]+)?href:[ \t]+reference/components/{Regex.Escape(fileName)}[ \t]*(?:#.*)?\r?$"))
            {
                failures.Add($"docs/toc.yml: missing page {fileName}.");
            }
            if (!files[statusPath].Contains($"`{page.Key}` | authored", StringComparison.Ordinal))
            {
                failures.Add($"{statusPath}: missing current authored status for {fileName}.");
            }
            foreach (string required in requiredContracts.GetValueOrDefault(page.Key) ?? [])
            {
                if (!Regex.IsMatch(page.Value, $@"(?m)^### `{Regex.Escape(required)}`\r?$"))
                {
                    failures.Add($"{page.Key}: missing source-referenced contract section {required}.");
                }
            }
            foreach (Match contextHeading in Regex.Matches(page.Value, @"(?m)^### `(?<name>\w+Context)`"))
            {
                string name = contextHeading.Groups["name"].Value;
                string? source = contracts.GetValueOrDefault(name);
                if (source is null)
                {
                    failures.Add($"{page.Key}: unresolved context {name}.");
                    continue;
                }
                string documented = Section(page.Value, $"### `{name}`");
                HashSet<string> expected = new(StringComparer.Ordinal);
                foreach (Match property in Regex.Matches(source, @"public (?<type>[^\r\n]+?) (?<name>\w+) (?:\{\s*get;\s*(?:(?:(?:private|protected|internal)\s+)*(?:set|init);\s*)?\}|=>[^\r\n]+;)"))
                {
                    string propertyName = property.Groups["name"].Value;
                    expected.Add(propertyName);
                    string? row = documented.Split('\n').FirstOrDefault(line => line.StartsWith($"| `{propertyName}` |", StringComparison.Ordinal));
                    if (row is null || !row.Split('|')[2].Contains($"`{property.Groups["type"].Value}`", StringComparison.Ordinal))
                    {
                        failures.Add($"{page.Key}: {name}.{propertyName} missing or stale context property/type.");
                    }
                }
                foreach (Match property in Regex.Matches(documented, @"(?m)^\| `(\w+)` \|"))
                {
                    if (!expected.Contains(property.Groups[1].Value))
                    {
                        failures.Add($"{page.Key}: {name} has stale context property {property.Groups[1].Value}.");
                    }
                }
            }
            foreach (Match heading in Regex.Matches(page.Value, @"(?m)^### `(?<name>\w+State|ProjectionConnectionStatus)`"))
            {
                string name = heading.Groups["name"].Value;
                if (rows.ContainsKey(name))
                {
                    continue;
                }
                string? source = contracts.GetValueOrDefault(name);
                if (source is null)
                {
                    failures.Add($"{page.Key}: unresolved state vocabulary {name}.");
                    continue;
                }
                string[] expected = Regex.Matches(source, @"(?m)^    (\w+)(?=[\t ]*(?:=|,|\r?$))")
                    .Select(match => match.Groups[1].Value).Order(StringComparer.Ordinal).ToArray();
                string[] actual = Regex.Matches(Section(page.Value, $"### `{name}`"), @"(?m)^\| `(\w+)` \|")
                    .Select(match => match.Groups[1].Value).Order(StringComparer.Ordinal).ToArray();
                if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
                {
                    failures.Add($"{page.Key}: {name} state vocabulary differs from source.");
                }
            }
        }
        return failures.Order(StringComparer.Ordinal).ToArray();
    }

    private static string Section(string text, string heading)
    {
        int start = text.IndexOf(heading + "\n", StringComparison.Ordinal);
        if (start < 0)
        {
            start = text.IndexOf(heading + "\r\n", StringComparison.Ordinal);
        }
        if (start < 0)
        {
            return string.Empty;
        }
        int level = heading.TakeWhile(character => character == '#').Count();
        string remaining = text[(start + heading.Length)..];
        Match end = Regex.Match(remaining, $@"(?m)^#{{1,{level}}} ");
        return end.Success ? remaining[..end.Index] : remaining;
    }

    private static string ContractTableRow(string contract, string area) {
        foreach (string line in contract.Split('\n')) {
            string trimmed = line.TrimStart();
            if (trimmed.StartsWith($"| {area} ", StringComparison.Ordinal)) {
                return line;
            }
        }

        return string.Empty;
    }

    private static IEnumerable<string> ComponentPageFiles() {
        string componentsDir = Path.Combine(ProjectRoot(), "docs", "reference", "components");
        return Directory
            .EnumerateFiles(componentsDir, "*.md", SearchOption.TopDirectoryOnly)
            .Where(file => !string.Equals(Path.GetFileName(file), "index.md", StringComparison.Ordinal))
            .OrderBy(file => file, StringComparer.Ordinal);
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

    private static string Relative(string path)
        => Path.GetRelativePath(ProjectRoot(), path).Replace('\\', '/');

    private static string Absolute(string relative)
        => Path.Combine(ProjectRoot(), relative.Replace('/', Path.DirectorySeparatorChar));

    private static string ProjectRoot() {
        DirectoryInfo directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.FrontComposer.slnx"))) {
            directory = directory.Parent!;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate project root.");
    }

    [GeneratedRegex("(?m)^```csharp(.*)$")]
    private static partial Regex CSharpFenceOpener();

    [GeneratedRegex("reason=\"[^\"]+\"")]
    private static partial Regex NoCompileReason();

    [GeneratedRegex("(?i)(tenant-id|api-key|client-secret|password)\\s*[:=]\\s*\\S")]
    private static partial Regex TenantOrSecretLiteral();
}
