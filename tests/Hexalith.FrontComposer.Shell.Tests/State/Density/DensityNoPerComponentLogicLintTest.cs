// ATDD RED PHASE — Story 3-3 Task 10.6a (D9, D10; AC6; ADR-041)
// Today: passes vacuously (no source contains the literals).
// Once Task 4 lands fc-density.js + the body[data-fc-density] CSS rules in FrontComposerShell.razor.css,
// the test enforces the single-source invariant: ONLY the four approved files may contain
// "--fc-density" or "data-fc-density".

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.Density;

/// <summary>
/// Story 3-3 Task 10.6a — single-source-of-truth lint for the density CSS variable
/// (ADR-041). Searches every source file under <c>src/Hexalith.FrontComposer.Shell/</c>
/// for the literals <c>--fc-density</c> and <c>data-fc-density</c>; each occurrence
/// must live in one of the approved owners or the test fails.
/// </summary>
public sealed class DensityNoPerComponentLogicLintTest
{
    private static readonly string[] _approvedFileSuffixes =
    [
        "Components\\Layout\\FrontComposerShell.razor.css",
        "Components/Layout/FrontComposerShell.razor.css",
        "Components\\Layout\\FcDensityApplier.razor",
        "Components/Layout/FcDensityApplier.razor",
        "Components\\Layout\\FcDensityApplier.razor.cs",
        "Components/Layout/FcDensityApplier.razor.cs",
        "Components\\Layout\\FcDensityPreviewPanel.razor",
        "Components/Layout/FcDensityPreviewPanel.razor",
        "Components\\Layout\\FcDensityPreviewPanel.razor.cs",
        "Components/Layout/FcDensityPreviewPanel.razor.cs",
        "Components\\Layout\\FcDensityPreviewPanel.razor.css",
        "Components/Layout/FcDensityPreviewPanel.razor.css",
        "wwwroot\\js\\fc-density.js",
        "wwwroot/js/fc-density.js",
    ];

    [Fact]
    public void SearchesSrcForRogueDensityVars()
    {
        DirectoryInfo? shellDir = LocateShellSourceDirectory();
        shellDir.ShouldNotBeNull("Could not locate src/Hexalith.FrontComposer.Shell from the test working directory.");

        IEnumerable<FileInfo> files = shellDir!
            .EnumerateFiles("*.*", SearchOption.AllDirectories)
            .Where(f =>
                !f.FullName.Contains("\\bin\\", StringComparison.Ordinal) &&
                !f.FullName.Contains("/bin/", StringComparison.Ordinal) &&
                !f.FullName.Contains("\\obj\\", StringComparison.Ordinal) &&
                !f.FullName.Contains("/obj/", StringComparison.Ordinal) &&
                (f.Extension is ".cs" or ".razor" or ".css" or ".js"));

        List<string> rogueOccurrences = [];
        foreach (FileInfo file in files)
        {
            string content = File.ReadAllText(file.FullName);
            if (!content.Contains("--fc-density", StringComparison.Ordinal) &&
                !content.Contains("data-fc-density", StringComparison.Ordinal))
            {
                continue;
            }

            bool isApproved = _approvedFileSuffixes.Any(suffix =>
                file.FullName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            if (!isApproved)
            {
                rogueOccurrences.Add(file.FullName);
            }
        }

        rogueOccurrences.ShouldBeEmpty(
            "ADR-041: --fc-density / data-fc-density must live ONLY in FrontComposerShell.razor.css, " +
            "FcDensityApplier, FcDensityPreviewPanel, or wwwroot/js/fc-density.js.");
    }

    private static DirectoryInfo? LocateShellSourceDirectory()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            string candidate = Path.Combine(dir.FullName, "src", "Hexalith.FrontComposer.Shell");
            if (Directory.Exists(candidate))
            {
                return new DirectoryInfo(candidate);
            }

            dir = dir.Parent;
        }

        return null;
    }
}
