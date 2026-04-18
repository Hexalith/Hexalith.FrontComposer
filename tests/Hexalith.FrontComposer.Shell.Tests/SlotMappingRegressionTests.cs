using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests;

/// <summary>
/// Story 3-1 semantic slot mapping regression lock.
/// </summary>
public sealed class SlotMappingRegressionTests
{
    [Fact]
    public void Binding_table_matches_verified_baseline()
    {
        string actual = string.Join("\n", [
            "CSS Slots",
            "--fc-color-accent = var(--accent-base-color)",
            "--fc-color-neutral = var(--colorNeutralForeground1)",
            "--fc-color-success = var(--colorStatusSuccessForeground1)",
            "--fc-color-warning = var(--colorStatusWarningForeground1)",
            "--fc-color-danger = var(--colorStatusDangerForeground1)",
            "--fc-color-info = var(--colorStatusInfoForeground1)",
            string.Empty,
            "Lifecycle Mapping",
            "Idle = Neutral",
            "Submitting = Accent",
            "Acknowledged = Neutral",
            "Syncing = Accent",
            "Confirmed = Success",
            "Rejected = Danger",
            string.Empty,
        ]);

        string baseline = File.ReadAllText(LocateBaseline()).Replace("\r\n", "\n");
        actual.ShouldBe(baseline);

        string css = File.ReadAllText(LocateCss()).Replace("\r\n", "\n");
        css.ShouldContain("--fc-color-accent: var(--accent-base-color);");
        css.ShouldContain("--fc-color-neutral: var(--colorNeutralForeground1);");
        css.ShouldContain("--fc-color-success: var(--colorStatusSuccessForeground1);");
        css.ShouldContain("--fc-color-warning: var(--colorStatusWarningForeground1);");
        css.ShouldContain("--fc-color-danger: var(--colorStatusDangerForeground1);");
        css.ShouldContain("--fc-color-info: var(--colorStatusInfoForeground1);");
    }

    private static string LocateBaseline()
    {
        DirectoryInfo? cursor = new(AppContext.BaseDirectory);
        while (cursor is not null)
        {
            string candidate = Path.Combine(cursor.FullName, "tests", "Hexalith.FrontComposer.Shell.Tests", "SlotMappingRegressionTests.BindingTable.verified.txt");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            cursor = cursor.Parent;
        }

        throw new FileNotFoundException("SlotMappingRegressionTests.BindingTable.verified.txt not found.");
    }

    private static string LocateCss()
    {
        DirectoryInfo? cursor = new(AppContext.BaseDirectory);
        while (cursor is not null)
        {
            string candidate = Path.Combine(cursor.FullName, "src", "Hexalith.FrontComposer.Shell", "Components", "Layout", "FrontComposerShell.razor.css");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            cursor = cursor.Parent;
        }

        throw new FileNotFoundException("FrontComposerShell.razor.css not found.");
    }
}
