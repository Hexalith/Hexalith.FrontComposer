// Story 3-3 Task 10.10a (Winston review — D16 / AC7).
// Pins the Story 3-4 IShortcutService migration contract by asserting exactly ONE inline
// @onkeydown binding on .fc-shell-root. Story 3-4 replaces this inline binding with the
// service-based registration; the service brings its own conflict-detection invariant, so this
// test is intentionally made obsolete at Story 3-4 time.

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

public sealed class CtrlCommaSingleBindingTest
{
    [Fact]
    public void FrontComposerShellRazorHasExactlyOneOnKeyDownBinding()
    {
        DirectoryInfo? shellDir = LocateShellSourceDirectory();
        shellDir.ShouldNotBeNull("Could not locate src/Hexalith.FrontComposer.Shell from the test working directory.");

        string razorPath = Path.Combine(shellDir!.FullName, "Components", "Layout", "FrontComposerShell.razor");
        File.Exists(razorPath).ShouldBeTrue($"FrontComposerShell.razor not found at {razorPath}.");

        string[] lines = File.ReadAllLines(razorPath);
        string[] matchingLines = [.. lines.Where(line => line.Contains("@onkeydown=\"HandleGlobalKeyDown\"", StringComparison.Ordinal))];

        matchingLines.Length.ShouldBe(
            1,
            "Story 3-3 D16 invariant: exactly one inline @onkeydown binding on .fc-shell-root. "
                + "Story 3-4 replaces this with IShortcutService.Register — do not add additional bindings in 3-3.");

        matchingLines[0].ShouldContain(
            "fc-shell-root",
            Case.Sensitive,
            "The single inline @onkeydown binding must stay on the shell root element until Story 3-4 migrates the shortcut to IShortcutService.");
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
