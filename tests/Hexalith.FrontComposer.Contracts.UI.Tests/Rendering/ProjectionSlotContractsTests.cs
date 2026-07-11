using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.UI.Tests.Rendering;

public sealed class ProjectionSlotContractsTests {
    [Fact]
    public void FieldSlotContext_SuppliedInputs_ExposesThem() {
        FieldDescriptor field = new("Priority", typeof(int).FullName!, false);
        RenderContext renderContext = new("tenant-a", "user-b", FcRenderMode.Server, DensityLevel.Compact, true) { IsDevMode = true };
        Projection projection = new(42);
        RenderFragment<FieldSlotContext<Projection, int>> fallback = static _ => static _ => { };

        FieldSlotContext<Projection, int> context = new(
            42, projection, field, renderContext, ProjectionRole.ActionQueue,
            DensityLevel.Compact, true, true, fallback);

        context.Value.ShouldBe(42);
        context.Parent.ShouldBe(projection);
        context.Field.ShouldBeSameAs(field);
        context.RenderContext.ShouldBeSameAs(renderContext);
        context.RenderDefault.ShouldBeSameAs(fallback);
    }

    [Fact]
    public void FieldSlotContext_NullRequiredArguments_Throws() {
        FieldDescriptor field = new("Priority", typeof(int).FullName!, false);
        RenderContext renderContext = new("tenant-a", "user-b", FcRenderMode.Server, DensityLevel.Comfortable, false);
        Projection projection = new(42);

        _ = Should.Throw<ArgumentNullException>(() => new FieldSlotContext<Projection, int>(42, null!, field, renderContext, null, DensityLevel.Comfortable, false, false, null));
        _ = Should.Throw<ArgumentNullException>(() => new FieldSlotContext<Projection, int>(42, projection, null!, renderContext, null, DensityLevel.Comfortable, false, false, null));
        _ = Should.Throw<ArgumentNullException>(() => new FieldSlotContext<Projection, int>(42, projection, field, null!, null, DensityLevel.Comfortable, false, false, null));
    }

    public sealed record Projection(int Priority);
}
