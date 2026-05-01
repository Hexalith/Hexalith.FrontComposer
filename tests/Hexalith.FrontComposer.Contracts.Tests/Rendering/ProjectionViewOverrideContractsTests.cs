using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Rendering;

public sealed class ProjectionViewOverrideContractsTests {
    [Fact]
    public void ProjectionViewOverrideContractVersion_IsPackedVersion() {
        ProjectionViewOverrideContractVersion.Current.ShouldBe(1_000_000);
        ProjectionViewOverrideContractVersion.Current.ShouldBe(
            (ProjectionViewOverrideContractVersion.Major * 1_000_000)
            + (ProjectionViewOverrideContractVersion.Minor * 1_000)
            + ProjectionViewOverrideContractVersion.Build);
    }

    [Fact]
    public void ProjectionViewOverrideDescriptor_StoresTypeOnlyMetadata() {
        ProjectionViewOverrideDescriptor descriptor = new(
            ProjectionType: typeof(ViewProjection),
            Role: ProjectionRole.DetailRecord,
            ComponentType: typeof(ValidReplacement),
            ContractVersion: ProjectionViewOverrideContractVersion.Current,
            RegistrationSource: "test");

        descriptor.ProjectionType.ShouldBe(typeof(ViewProjection));
        descriptor.Role.ShouldBe(ProjectionRole.DetailRecord);
        descriptor.ComponentType.ShouldBe(typeof(ValidReplacement));
        descriptor.ContractVersion.ShouldBe(ProjectionViewOverrideContractVersion.Current);
        descriptor.RegistrationSource.ShouldBe("test");
    }

    [Fact]
    public void ProjectionViewContext_CarriesPerRenderInputs_AndDefaultDelegates() {
        RenderContext renderContext = new("tenant-a", "user-b", FcRenderMode.Server, DensityLevel.Compact, IsReadOnly: true);
        ViewProjection[] items = [new(1, "Alpha")];
        RenderFragment defaultBody = static _ => { };

        ProjectionViewContext<ViewProjection> context = new(
            projectionType: typeof(ViewProjection),
            boundedContext: "Counter",
            role: null,
            items: items,
            renderContext: renderContext,
            columns: [new ProjectionTemplateColumnDescriptor("Name", "Name", null, "Display name")],
            sections: [new ProjectionTemplateSectionDescriptor("Body", "Body", "Body")],
            lifecycleState: "Loaded",
            entityLabel: "Counter",
            entityPluralLabel: "Counters",
            defaultBody: defaultBody,
            sectionRenderer: static _ => static _ => { },
            rowRenderer: static _ => static _ => { },
            fieldRenderer: static (_, _) => static _ => { });

        context.ProjectionType.ShouldBe(typeof(ViewProjection));
        context.BoundedContext.ShouldBe("Counter");
        context.Role.ShouldBeNull();
        context.Items.ShouldBeSameAs(items);
        context.RenderContext.ShouldBeSameAs(renderContext);
        context.Columns.ShouldHaveSingleItem().Description.ShouldBe("Display name");
        context.Sections.ShouldHaveSingleItem().Name.ShouldBe("Body");
        context.LifecycleState.ShouldBe("Loaded");
        context.EntityLabel.ShouldBe("Counter");
        context.EntityPluralLabel.ShouldBe("Counters");
        context.DefaultBody.ShouldBeSameAs(defaultBody);
    }

    public sealed record ViewProjection(int Id, string Name);

    public sealed class ValidReplacement : ComponentBase {
        [Parameter]
        public ProjectionViewContext<ViewProjection> Context { get; set; } = default!;
    }
}
