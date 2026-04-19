using System.Reflection;

using Hexalith.FrontComposer.Shell.Components.Layout;

using Microsoft.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Parameter-surface lock for the framework-owned shell component.
/// Story 3-1 shipped 6 parameters. Story 3-2 Task 10.9 / D10 adds <c>HeaderCenter</c> as the
/// single append at index 1 (between HeaderStart and HeaderEnd, mirroring the L→R visual header
/// layout). Append-only discipline: no rename, retype, removal, or reorder of existing parameters.
/// </summary>
public sealed class FrontComposerShellParameterSurfaceTests
{
    [Fact]
    public void Parameter_surface_matches_story_3_2_contract()
    {
        // ATDD RED PHASE — this array encodes the post-3-2 surface. It will diverge from the
        // current 6-parameter implementation until Story 3-2 Task 8.4 adds HeaderCenter.
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
        ]);
    }
}
