using System.Text.RegularExpressions;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Architecture;

/// <summary>
/// Story 1.6 AC3 (NFR9 / ADR-007) cross-cutting single-writer pin. Asserts the
/// <b>per-slice persistence ownership</b> that the global <see cref="NFR17ComplianceTripwireTests"/>
/// call-site count (== 7 across all of <c>State/</c>) does not by itself guarantee: that each of the
/// Theme and Density slices keeps exactly one persistence writer, and that the Theme state field has
/// a single reducer assigning it.
/// </summary>
/// <remarks>
/// <para>
/// Scope of the pin (deliberately the invariants that are <i>literally true</i> at the source):
/// </para>
/// <list type="bullet">
///   <item><description><b>Theme persistence</b> — exactly one <c>storage.SetAsync</c> call site under
///     <c>State/Theme/</c>, and it lives in <c>ThemeEffects.cs</c>.</description></item>
///   <item><description><b>Density persistence</b> — every <c>storage.SetAsync</c> call site under
///     <c>State/Density/</c> lives in <c>DensityEffects.cs</c> (one effect class owns both the direct
///     write and the legacy-migration re-write).</description></item>
///   <item><description><b>Theme state field</b> — <c>CurrentTheme</c> is assigned in exactly one
///     reducer (<c>ThemeReducers.cs</c>).</description></item>
/// </list>
/// <para>
/// Two AC3-related facts are intentionally NOT pinned here because they are multi-writer by design,
/// and pinning them would be a false claim:
/// </para>
/// <list type="bullet">
///   <item><description>Theme DOM application (<c>IThemeService.SetThemeAsync</c>) is invoked from three
///     sites — the effect (user change), <c>FrontComposerShell.ApplyThemeAsync</c> (re-apply on render),
///     and <c>FcSystemThemeWatcher</c> (OS-follow when <c>CurrentTheme == System</c>) — all reading the
///     single <c>CurrentTheme</c> field. The single-writer guarantee is on persistence + state, not on
///     the read-driven re-apply.</description></item>
///   <item><description>Density <c>UserPreference</c> is assigned by four reducer methods
///     (changed / cleared / hydrated / legacy-changed); the <c>data-fc-density</c> DOM single-writer is
///     pinned separately by <c>DensityNoPerComponentLogicLintTest</c> (ADR-041).</description></item>
/// </list>
/// </remarks>
public sealed class SliceSingleWriterGovernanceTests
{
    private static readonly Regex SetAsyncCall = new(
        @"(?:storage|_storage)\.SetAsync\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Assignment of CurrentTheme (a 'with { CurrentTheme = ... }' or direct assign), excluding '=='.
    private static readonly Regex CurrentThemeAssignment = new(
        @"\bCurrentTheme\s*=(?!=)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void ThemePersistence_HasSingleWriter_InThemeEffects()
    {
        List<(string file, int count)> writers = SetAsyncCallSites(SliceFolder("Theme"));

        int total = writers.Sum(w => w.count);
        total.ShouldBe(
            1,
            "AC3/NFR9 — the Theme slice must keep exactly one persistence writer. "
            + "If a second SetAsync was added under State/Theme/, single-writer discipline (ADR-007) "
            + "is broken. Writers: " + Describe(writers));
        writers.Single(w => w.count > 0).file.ShouldBe(
            "ThemeEffects.cs",
            "AC3/NFR9 — theme persistence must live in ThemeEffects.HandleThemeChanged, not elsewhere.");
    }

    [Fact]
    public void DensityPersistence_AllWritersLiveIn_DensityEffects()
    {
        List<(string file, int count)> writers = SetAsyncCallSites(SliceFolder("Density"));

        int total = writers.Sum(w => w.count);
        total.ShouldBe(
            2,
            "AC3/NFR9 — the Density slice has exactly two SetAsync sites (direct write + legacy "
            + "migration re-write), both in DensityEffects. Writers: " + Describe(writers));
        writers
            .Where(w => w.count > 0)
            .Select(w => w.file)
            .ShouldBe(
                ["DensityEffects.cs"],
                "AC3/NFR9 — density persistence must live solely in DensityEffects (one effect class "
                + "owns both the direct and migration writes).");
    }

    [Fact]
    public void CurrentTheme_AssignedBy_SingleReducer()
    {
        List<(string file, int count)> assigners = AssignmentSites(SliceFolder("Theme"), CurrentThemeAssignment);

        int total = assigners.Sum(a => a.count);
        total.ShouldBe(
            1,
            "AC3/NFR9 — CurrentTheme must be assigned by exactly one reducer (single-writer state field). "
            + "Assigners: " + Describe(assigners));
        assigners.Single(a => a.count > 0).file.ShouldBe(
            "ThemeReducers.cs",
            "AC3/NFR9 — CurrentTheme is owned by ThemeReducers.ReduceThemeChanged.");
    }

    private static List<(string file, int count)> SetAsyncCallSites(string folder)
        => Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories)
            .Select(f => (file: Path.GetFileName(f), count: SetAsyncCall.Matches(File.ReadAllText(f)).Count))
            .Where(x => x.count > 0)
            .ToList();

    private static List<(string file, int count)> AssignmentSites(string folder, Regex pattern)
    {
        List<(string file, int count)> sites = [];
        foreach (string f in Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories))
        {
            int count = File.ReadLines(f)
                .Where(line => !line.TrimStart().StartsWith("///", StringComparison.Ordinal))
                .Sum(line => pattern.Matches(line).Count);
            if (count > 0)
            {
                sites.Add((Path.GetFileName(f), count));
            }
        }

        return sites;
    }

    private static string Describe(IEnumerable<(string file, int count)> sites)
        => sites.Count() == 0
            ? "(none)"
            : string.Join(", ", sites.Select(s => $"{s.file}×{s.count}"));

    private static string SliceFolder(string slice)
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            string candidate = Path.Combine(current, "src", "Hexalith.FrontComposer.Shell", "State", slice);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            DirectoryInfo? parent = Directory.GetParent(current);
            if (parent is null)
            {
                break;
            }

            current = parent.FullName;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate src/Hexalith.FrontComposer.Shell/State/{slice}/ from test base directory.");
    }
}
