using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Rendering;

/// <summary>
/// Story 6-2 T10 — contract/attribute/descriptor invariants for the Level 2 typed
/// projection-template surface.
/// </summary>
public sealed class ProjectionTemplateContractsTests {
    [Fact]
    public void Current_PacksMajorMinorBuild() {
        // 1.0.0 → 1_000_000
        ProjectionTemplateContractVersion.Current.ShouldBe(1_000_000);
        ProjectionTemplateContractVersion.Major.ShouldBe(1);
        ProjectionTemplateContractVersion.Minor.ShouldBe(0);
        ProjectionTemplateContractVersion.Build.ShouldBe(0);
    }

    [Fact]
    public void Attribute_NullProjectionType_Throws() {
        Should.Throw<ArgumentNullException>(() =>
            new ProjectionTemplateAttribute(projectionType: null!, expectedContractVersion: 1));
    }

    [Fact]
    public void Attribute_StoresProjectionTypeAndContractVersion() {
        ProjectionTemplateAttribute attr = new(typeof(string), 1_000_000);
        attr.ProjectionType.ShouldBe(typeof(string));
        attr.ExpectedContractVersion.ShouldBe(1_000_000);
        attr.Role.ShouldBe(ProjectionRole.ActionQueue); // numeric default 0
    }

    [Fact]
    public void Descriptor_StructuralEquality() {
        ProjectionTemplateDescriptor a = new(
            ProjectionType: typeof(string),
            Role: null,
            TemplateType: typeof(int),
            ContractVersion: 1_000_000);
        ProjectionTemplateDescriptor b = new(
            ProjectionType: typeof(string),
            Role: null,
            TemplateType: typeof(int),
            ContractVersion: 1_000_000);
        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
    }

    [Fact]
    public void Context_NullArguments_Throw() {
        RenderFragment defaultBody = static _ => { };
        ProjectionTemplateSectionRenderer sectionRenderer = static _ => static _ => { };
        ProjectionTemplateRowRenderer<string> rowRenderer = static _ => static _ => { };
        ProjectionTemplateFieldRenderer<string> renderer = static (_, _) => static _ => { };
        IReadOnlyList<string> items = [];
        IReadOnlyList<ProjectionTemplateColumnDescriptor> columns = [];
        IReadOnlyList<ProjectionTemplateSectionDescriptor> sections = [];

        Should.Throw<ArgumentNullException>(() =>
            new ProjectionTemplateContext<string>(
                projectionType: null!,
                boundedContext: null,
                role: null,
                renderContext: null,
                items: items,
                columns: columns,
                sections: sections,
                defaultBody: defaultBody,
                sectionRenderer: sectionRenderer,
                rowRenderer: rowRenderer,
                fieldRenderer: renderer));

        Should.Throw<ArgumentNullException>(() =>
            new ProjectionTemplateContext<string>(
                projectionType: typeof(string),
                boundedContext: null,
                role: null,
                renderContext: null,
                items: null!,
                columns: columns,
                sections: sections,
                defaultBody: defaultBody,
                sectionRenderer: sectionRenderer,
                rowRenderer: rowRenderer,
                fieldRenderer: renderer));

        Should.Throw<ArgumentNullException>(() =>
            new ProjectionTemplateContext<string>(
                projectionType: typeof(string),
                boundedContext: null,
                role: null,
                renderContext: null,
                items: items,
                columns: null!,
                sections: sections,
                defaultBody: defaultBody,
                sectionRenderer: sectionRenderer,
                rowRenderer: rowRenderer,
                fieldRenderer: renderer));

        Should.Throw<ArgumentNullException>(() =>
            new ProjectionTemplateContext<string>(
                projectionType: typeof(string),
                boundedContext: null,
                role: null,
                renderContext: null,
                items: items,
                columns: columns,
                sections: null!,
                defaultBody: defaultBody,
                sectionRenderer: sectionRenderer,
                rowRenderer: rowRenderer,
                fieldRenderer: renderer));

        Should.Throw<ArgumentNullException>(() =>
            new ProjectionTemplateContext<string>(
                projectionType: typeof(string),
                boundedContext: null,
                role: null,
                renderContext: null,
                items: items,
                columns: columns,
                sections: sections,
                defaultBody: null!,
                sectionRenderer: sectionRenderer,
                rowRenderer: rowRenderer,
                fieldRenderer: renderer));

        Should.Throw<ArgumentNullException>(() =>
            new ProjectionTemplateContext<string>(
                projectionType: typeof(string),
                boundedContext: null,
                role: null,
                renderContext: null,
                items: items,
                columns: columns,
                sections: sections,
                defaultBody: defaultBody,
                sectionRenderer: null!,
                rowRenderer: rowRenderer,
                fieldRenderer: renderer));

        Should.Throw<ArgumentNullException>(() =>
            new ProjectionTemplateContext<string>(
                projectionType: typeof(string),
                boundedContext: null,
                role: null,
                renderContext: null,
                items: items,
                columns: columns,
                sections: sections,
                defaultBody: defaultBody,
                sectionRenderer: sectionRenderer,
                rowRenderer: null!,
                fieldRenderer: renderer));

        Should.Throw<ArgumentNullException>(() =>
            new ProjectionTemplateContext<string>(
                projectionType: typeof(string),
                boundedContext: null,
                role: null,
                renderContext: null,
                items: items,
                columns: columns,
                sections: sections,
                defaultBody: defaultBody,
                sectionRenderer: sectionRenderer,
                rowRenderer: rowRenderer,
                fieldRenderer: null!));
    }

    [Fact]
    public void Context_ExposesSuppliedInputs() {
        RenderFragment defaultBody = static _ => { };
        ProjectionTemplateSectionRenderer sectionRenderer = static _ => static _ => { };
        ProjectionTemplateRowRenderer<string> rowRenderer = static _ => static _ => { };
        ProjectionTemplateFieldRenderer<string> renderer = static (_, _) => static _ => { };
        IReadOnlyList<string> items = ["a", "b"];
        IReadOnlyList<ProjectionTemplateColumnDescriptor> columns =
        [
            new("Id", "Id", null, null),
            new("Name", "Name", 1, "The display name"),
        ];
        IReadOnlyList<ProjectionTemplateSectionDescriptor> sections =
        [
            new("Body", "Body", "Body"),
            new("Row", "Row", "Row"),
        ];
        RenderContext rc = new("tenant-x", "user-y", FcRenderMode.Server, DensityLevel.Comfortable, IsReadOnly: false);

        ProjectionTemplateContext<string> ctx = new(
            projectionType: typeof(string),
            boundedContext: "Demo",
            role: ProjectionRole.StatusOverview,
            renderContext: rc,
            items: items,
            columns: columns,
            sections: sections,
            defaultBody: defaultBody,
            sectionRenderer: sectionRenderer,
            rowRenderer: rowRenderer,
            fieldRenderer: renderer);

        ctx.ProjectionType.ShouldBe(typeof(string));
        ctx.BoundedContext.ShouldBe("Demo");
        ctx.Role.ShouldBe(ProjectionRole.StatusOverview);
        ctx.RenderContext.ShouldBe(rc);
        ctx.Items.ShouldBe(items);
        ctx.Columns.ShouldBe(columns);
        ctx.Sections.ShouldBe(sections);
        ctx.DefaultBody.ShouldBeSameAs(defaultBody);
        ctx.SectionRenderer.ShouldBeSameAs(sectionRenderer);
        ctx.RowRenderer.ShouldBeSameAs(rowRenderer);
        ctx.FieldRenderer.ShouldBeSameAs(renderer);
    }
}
