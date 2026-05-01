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

    [Fact]
    public void ProjectionViewContext_AC13_StarterShape_ExposesAllStableInputsSimultaneously() {
        // P15 — pin the AC13 starter-shape contract that Story 6-5 will consume. All listed
        // members must be reachable on a representative render so a silent trim breaks this
        // test before it surprises 6-5's overlay/clipboard generator.
        RenderContext renderContext = new("tenant-c", "user-d", FcRenderMode.Server, DensityLevel.Roomy, IsReadOnly: false);
        ViewProjection[] items = [new(2, "Beta"), new(3, "Gamma")];
        ProjectionTemplateColumnDescriptor[] columns = [
            new ProjectionTemplateColumnDescriptor("Id", "Id", 1, "Identifier"),
            new ProjectionTemplateColumnDescriptor("Name", "Name", 2, "Display name"),
        ];
        ProjectionTemplateSectionDescriptor[] sections = [
            new ProjectionTemplateSectionDescriptor("Body", "Body", "Body"),
        ];

        ProjectionViewContext<ViewProjection> context = new(
            projectionType: typeof(ViewProjection),
            boundedContext: "Counter",
            role: ProjectionRole.DetailRecord,
            items: items,
            renderContext: renderContext,
            columns: columns,
            sections: sections,
            lifecycleState: "Loaded",
            entityLabel: "Counter",
            entityPluralLabel: "Counters",
            defaultBody: static _ => { },
            sectionRenderer: static _ => static _ => { },
            rowRenderer: static _ => static _ => { },
            fieldRenderer: static (_, _) => static _ => { });

        // Projection metadata — projection type, bounded context, role.
        context.ProjectionType.ShouldNotBeNull();
        context.BoundedContext.ShouldNotBeNullOrWhiteSpace();
        context.Role.ShouldNotBeNull();

        // Field/section descriptors — non-null collections, both with at least one entry.
        context.Columns.ShouldNotBeNull();
        context.Columns.ShouldNotBeEmpty();
        context.Sections.ShouldNotBeNull();
        context.Sections.ShouldNotBeEmpty();

        // Localization-safe accessible names/labels/help text — non-null and non-whitespace.
        context.EntityLabel.ShouldNotBeNullOrWhiteSpace();
        context.EntityPluralLabel.ShouldNotBeNullOrWhiteSpace();
        context.LifecycleState.ShouldNotBeNullOrWhiteSpace();
        context.Columns[0].Description.ShouldNotBeNullOrWhiteSpace();

        // Current render flags — RenderContext non-null with tenant/user/density/read-only.
        context.RenderContext.ShouldNotBeNull();
        context.DensityLevel.ShouldBe(DensityLevel.Roomy);
        context.IsReadOnly.ShouldBeFalse();

        // Safe default-render delegates — all four reachable and non-null.
        context.DefaultBody.ShouldNotBeNull();
        context.SectionRenderer.ShouldNotBeNull();
        context.RowRenderer.ShouldNotBeNull();
        context.FieldRenderer.ShouldNotBeNull();
    }

    public sealed record ViewProjection(int Id, string Name);

    public sealed class ValidReplacement : ComponentBase {
        [Parameter]
        public ProjectionViewContext<ViewProjection> Context { get; set; } = default!;
    }
}
