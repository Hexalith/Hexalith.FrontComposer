using System.Reflection;

using Hexalith.FrontComposer.Shell.Components.Layout;

using Microsoft.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Parameter-surface lock for the framework-owned shell component.
/// Story 3-1 shipped 6 parameters. Story 3-2 Task 10.9 / D10 adds <c>HeaderCenter</c> as the
/// single append at index 1 (between HeaderStart and HeaderEnd, mirroring the L→R visual header
/// layout). Handoff <c>frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening</c>
/// (outcome 1) appends <c>ContentLabel</c> and <c>ContentLabelledBy</c> at the tail so the shell's
/// single <c>#fc-main-content</c> <c>main</c> landmark can carry an accessible name.
/// Append-only discipline: no rename, retype, removal, or reorder of existing parameters.
/// </summary>
public sealed class FrontComposerShellParameterSurfaceTests {
    [Fact]
    public void Parameter_surface_matches_story_3_2_contract() {
        string[] actual = [..
            typeof(FrontComposerShell)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                .OrderBy(p => p.MetadataToken)
                .Select(p => $"{p.Name}:{p.PropertyType.Name}")];

        actual.ShouldBe([
            "HeaderStart:RenderFragment",
            "HeaderCenter:RenderFragment",
            "HeaderEnd:RenderFragment",
            "Navigation:RenderFragment",
            "Footer:RenderFragment",
            "ChildContent:RenderFragment",
            "AppTitle:String",
            // Handoff outcome 1 — append-only landmark-naming parameters (safe-default null).
            "ContentLabel:String",
            "ContentLabelledBy:String",
        ]);
    }
}
