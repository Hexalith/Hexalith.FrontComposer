using System.Reflection;

using Hexalith.FrontComposer.Shell.Components.Layout;

using Microsoft.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Story 3-1 parameter surface lock for the framework-owned shell component.
/// </summary>
public sealed class FrontComposerShellParameterSurfaceTests
{
    [Fact]
    public void Parameter_surface_matches_story_3_1_contract()
    {
        string[] actual = [..
            typeof(FrontComposerShell)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                .OrderBy(p => p.MetadataToken)
                .Select(p => $"{p.Name}:{p.PropertyType.Name}")];

        actual.ShouldBe([
            "HeaderStart:RenderFragment",
            "HeaderEnd:RenderFragment",
            "Navigation:RenderFragment",
            "Footer:RenderFragment",
            "ChildContent:RenderFragment",
            "AppTitle:String",
        ]);
    }
}
