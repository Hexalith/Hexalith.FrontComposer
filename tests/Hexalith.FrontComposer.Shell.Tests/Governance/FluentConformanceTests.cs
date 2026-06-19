using System.Text.RegularExpressions;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Governance;

/// <summary>
/// Governance guard for the project-wide "every UI page/component uses FrontComposer or Fluent v5 only"
/// rule (architecture.md §4.1). In Fluent UI Blazor v5 the design system only styles its own custom
/// elements (<c>&lt;fluent-button&gt;</c>, <c>&lt;fluent-text-input&gt;</c>, …); a raw <c>&lt;button&gt;</c> /
/// <c>&lt;input&gt;</c> / <c>&lt;select&gt;</c> / <c>&lt;textarea&gt;</c> is never upgraded and falls back to
/// unstyled browser rendering, which also drops the NFR6 accessibility affordances the Fluent components
/// provide. These tests fail the build if a raw interactive HTML control is reintroduced into the framework
/// Shell or the Counter sample web app, mirroring the Tenants.UI guard
/// (<c>Hexalith.Tenants.UI.Tests.DomainUiFluentConformanceTests</c>). Raw <c>&lt;a&gt;</c> navigation links
/// are permitted. Documented carve-outs are allowlisted below and in architecture.md §4.1.
/// <para>
/// A second guard enforces the §4.1 "no theme redefinition" rule: hand-authored styles must express
/// typography/color/spacing through Fluent 2 design tokens (<c>--colorNeutralForeground*</c>,
/// <c>--fontSizeBase*</c>, …) or Fluent component parameters, never legacy Fluent v4 / FAST tokens
/// (<c>--type-ramp-*</c>, <c>--neutral-*</c>, <c>--accent-*</c>, <c>--palette-*</c>, …). Pre-v5 files
/// still referencing legacy tokens are an allowlisted migration backlog that may only shrink.
/// </para>
/// </summary>
[Trait("Category", "Governance")]
public sealed class FluentConformanceTests {
    // Matches an opening tag for a raw interactive HTML control. The trailing character class anchors on a
    // real tag boundary (whitespace, self-close, or '>') so attributes like `inputmode=` and Fluent
    // components like <FluentButton> / <FluentTextInput> (capitalised) are not matched. Case-sensitive on
    // purpose — Razor component tags are PascalCase, raw HTML controls are lowercase.
    private static readonly Regex RawInteractiveControl = new(
        "<(button|input|select|textarea)(\\s|/|>)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Matches a legacy Fluent v4 / FAST design token (kebab-case CSS custom property). Fluent 2 tokens used
    // by Fluent UI Blazor v5 are camelCase (--colorNeutralForeground1, --fontSizeBase300, …) and the
    // project's own bridge variables are --fc-* / --layout-*, so neither is matched. Case-sensitive on
    // purpose — legacy FAST tokens are always lowercase-kebab.
    private static readonly Regex LegacyFluentToken = new(
        "--(type-ramp|neutral-foreground|neutral-fill|neutral-stroke|neutral-layer|accent-fill|"
        + "accent-foreground|accent-stroke|accent-base|palette-|design-unit|elevation-shadow|corner-radius|"
        + "focus-stroke|stroke-width|disabled-opacity)[a-z0-9-]*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Shell_components_use_fluent_v5_only_except_documented_carveouts() {
        string root = Path.Combine(RepositoryRoot(), "src", "Hexalith.FrontComposer.Shell");

        // Documented carve-out (architecture.md §4.1): FcHomeCard is framework chrome — a full-card link
        // button (role="link" + custom keyboard activation, scoped .fc-home-card-button CSS) that hosts an
        // <h2> + projection <ul> a FluentButton cannot contain without visual regression. It is fully
        // styled and accessible, so it is not the unstyled-control defect this rule targets.
        string[] carveOuts = ["FcHomeCard.razor"];

        AssertNoRawControls(root, carveOuts);
    }

    [Fact]
    public void CounterWeb_components_use_fluent_v5_only() {
        // The Counter *web app* is a shipped UI surface and must be Fluent-clean. The Counter.Specimens
        // project is deliberately excluded: its raw controls ARE the a11y/visual specimen fixtures.
        string root = Path.Combine(RepositoryRoot(), "samples", "Counter", "Counter.Web");

        AssertNoRawControls(root, carveOuts: []);
    }

    [Fact]
    public void Shell_styles_use_no_legacy_fluent_v4_tokens_except_migration_backlog() {
        string root = Path.Combine(RepositoryRoot(), "src", "Hexalith.FrontComposer.Shell");

        // Migration backlog (architecture.md §4.1): scoped/global styles still referencing Fluent v4 / FAST
        // design tokens instead of Fluent 2 tokens (--colorNeutralForeground*, --fontSizeBase*, …) or Fluent
        // component parameters. The allowlist may only shrink: migrating a file off legacy tokens requires
        // deleting its entry here — the stale-entry assertion in AssertNoLegacyTokens enforces it. The backlog
        // has now been fully burned down (correct-course 2026-06-19): every pre-v5 Shell stylesheet expresses
        // typography/color/spacing through Fluent 2 tokens, and density-coupled spacing uses the project's
        // --fc-spacing-unit (ADR-041) rather than the undefined legacy --design-unit. The list is empty, so the
        // guard now blocks ANY legacy-token usage anywhere in the Shell. Do not add entries: author new styles
        // with Fluent 2 tokens / Fluent component parameters.
        string[] migrationBacklog = [];

        AssertNoLegacyTokens(root, migrationBacklog);
    }

    private static void AssertNoRawControls(string root, string[] carveOuts) {
        Directory.Exists(root).ShouldBeTrue($"Fluent conformance scan root not found: {root}");

        EnumerationOptions options = new() {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.Hidden,
            IgnoreInaccessible = true,
        };

        string[] razorFiles = Directory
            .EnumerateFiles(root, "*.razor", options)
            .Where(f => !IsBuildOutput(f))
            .ToArray();

        // Guard against a broken path silently passing the scan.
        razorFiles.ShouldNotBeEmpty($"no .razor files found under {root}");

        List<string> offenders = [];
        foreach (string file in razorFiles) {
            if (carveOuts.Contains(Path.GetFileName(file), StringComparer.Ordinal)) {
                continue;
            }

            MatchCollection matches = RawInteractiveControl.Matches(File.ReadAllText(file));
            if (matches.Count > 0) {
                string tags = string.Join(
                    ", ",
                    matches.Select(match => match.Groups[1].Value).Distinct(StringComparer.Ordinal));
                offenders.Add($"{Path.GetFileName(file)} ({tags})");
            }
        }

        offenders.ShouldBeEmpty(
            "UI .razor components must use FrontComposer/Fluent v5 components only (no raw <button>/<input>/"
            + "<select>/<textarea>; raw <a> nav links allowed). Carve-outs are allowlisted in architecture.md "
            + $"§4.1. Raw interactive controls found in: {string.Join("; ", offenders)}");
    }

    private static void AssertNoLegacyTokens(string root, string[] migrationBacklog) {
        Directory.Exists(root).ShouldBeTrue($"Legacy-token scan root not found: {root}");

        EnumerationOptions options = new() {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.Hidden,
            IgnoreInaccessible = true,
        };

        string[] styleFiles = Directory
            .EnumerateFiles(root, "*.css", options)
            .Concat(Directory.EnumerateFiles(root, "*.razor", options))
            .Where(f => !IsBuildOutput(f))
            .ToArray();

        // Guard against a broken path silently passing the scan.
        styleFiles.ShouldNotBeEmpty($"no .css/.razor files found under {root}");

        HashSet<string> allowed = new(migrationBacklog, StringComparer.Ordinal);
        List<string> offenders = [];
        List<string> migrated = [];

        foreach (string file in styleFiles) {
            string relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            bool usesLegacyToken = LegacyFluentToken.IsMatch(File.ReadAllText(file));

            if (allowed.Contains(relative)) {
                if (!usesLegacyToken) {
                    migrated.Add(relative);
                }

                continue;
            }

            if (usesLegacyToken) {
                offenders.Add(relative);
            }
        }

        offenders.ShouldBeEmpty(
            "UI styles must express typography/color/spacing via Fluent 2 tokens (--colorNeutralForeground*, "
            + "--fontSizeBase*, --lineHeightBase*) or Fluent component parameters — never legacy Fluent v4 / "
            + "FAST tokens (--type-ramp-*, --neutral-*, --accent-*, --palette-*, …). See architecture.md §4.1. "
            + $"Legacy tokens found in: {string.Join("; ", offenders)}");

        migrated.ShouldBeEmpty(
            "These files no longer use legacy Fluent v4 tokens — remove them from the migrationBacklog "
            + $"allowlist so the backlog only shrinks: {string.Join("; ", migrated)}");
    }

    private static bool IsBuildOutput(string file) {
        string normalized = file.Replace('\\', '/');
        return normalized.Contains("/bin/", StringComparison.Ordinal)
            || normalized.Contains("/obj/", StringComparison.Ordinal);
    }

    private static string RepositoryRoot() {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null) {
            if (File.Exists(Path.Combine(dir.FullName, "Hexalith.FrontComposer.slnx"))) {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
