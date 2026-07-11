using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.UI.Tests.Rendering;

public sealed class ProjectionViewOverrideContractsTests {
    [Fact]
    public void ProjectionViewContext_PerRenderInputs_ExposesStableSurface() {
        RenderContext renderContext = new("tenant-a", "user-b", FcRenderMode.Server, DensityLevel.Compact, true) { IsDevMode = true };
        Projection[] items = [new(1, "Alpha")];
        RenderFragment defaultBody = static _ => { };

        ProjectionViewContext<Projection> context = new(
            typeof(Projection), "Counter", null, items, renderContext,
            [new ProjectionTemplateColumnDescriptor("Name", "Name", null, "Display name")],
            [new ProjectionTemplateSectionDescriptor("Body", "Body", "Body")],
            "Loaded", "Counter", "Counters", defaultBody,
            static _ => static _ => { }, static _ => static _ => { }, static (_, _) => static _ => { });

        context.Items.ShouldBeSameAs(items);
        context.DensityLevel.ShouldBe(DensityLevel.Compact);
        context.IsReadOnly.ShouldBeTrue();
        context.IsDevMode.ShouldBeTrue();
        context.DefaultBody.ShouldBeSameAs(defaultBody);
    }

    public sealed record Projection(int Id, string Name);
}
