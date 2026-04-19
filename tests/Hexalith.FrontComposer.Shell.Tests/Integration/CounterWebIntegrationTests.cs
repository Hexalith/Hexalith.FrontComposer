// ATDD RED PHASE — Story 3-2 Task 10.10 (D17; AC7)
// Fails at assertion time until Task 9 rewires Counter.Web MainLayout.razor to its three-line form.

using System.Text.RegularExpressions;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Integration;

/// <summary>
/// Story 3-2 Task 10.10 — Counter.Web MainLayout.razor file-shape regression.
/// D17 — after Task 9, MainLayout.razor is exactly three substantive lines:
///   @inherits LayoutComponentBase
///   @using Hexalith.FrontComposer.Shell.Components.Layout
///   &lt;FrontComposerShell&gt;@Body&lt;/FrontComposerShell&gt;
/// The @inject IFrontComposerRegistry line is REMOVED. The framework sidebar auto-populates via D18.
/// </summary>
public sealed class CounterWebIntegrationTests
{
    [Fact]
    public void MainLayoutIsThreeSubstantiveLines()
    {
        string projectRoot = FindRepoRoot();
        string path = Path.Combine(projectRoot, "samples", "Counter", "Counter.Web", "Components", "Layout", "MainLayout.razor");
        File.Exists(path).ShouldBeTrue($"Expected Counter.Web MainLayout.razor at {path}");

        string[] substantiveLines = [..
            File.ReadAllLines(path)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0 && !l.StartsWith("@*", StringComparison.Ordinal))];

        substantiveLines.Length.ShouldBe(3,
            $"Counter.Web MainLayout.razor MUST be exactly three substantive lines after Task 9 (D17). Found:\n{string.Join("\n", substantiveLines)}");

        substantiveLines[0].ShouldBe("@inherits LayoutComponentBase");
        substantiveLines[1].ShouldBe("@using Hexalith.FrontComposer.Shell.Components.Layout");

        // Line 3 is the FrontComposerShell invocation. Accept either the simple or ChildContent form,
        // but REJECT any @inject directive, any explicit Navigation slot, or any FluentNav block.
        // F15 — tightened: anchor to start/end of line + allow only whitespace between the tag and @Body.
        substantiveLines[2].ShouldMatch(@"^<FrontComposerShell>\s*@Body\s*</FrontComposerShell>$",
            "Line 3 must be <FrontComposerShell>@Body</FrontComposerShell> — no Navigation slot, no FluentNav (D17).");

        string body = string.Join("\n", substantiveLines);
        body.ShouldNotContain("@inject", Case.Sensitive, "D17: the @inject IFrontComposerRegistry line must be removed.");
        body.ShouldNotContain("<FluentNav", Case.Sensitive, "D17: the adopter-authored FluentNav block must be removed.");
        body.ShouldNotContain("<Navigation>", Case.Sensitive, "D17: explicit Navigation slot must be removed; framework auto-populates via D18.");
    }

    private static string FindRepoRoot()
    {
        // Walks up from the test binary dir until Hexalith.FrontComposer.sln is found.
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.GetFiles("Hexalith.FrontComposer.sln").Length > 0)
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate Hexalith.FrontComposer.sln by walking up from " + AppContext.BaseDirectory);
    }
}
