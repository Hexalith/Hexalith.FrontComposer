using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.UI.Tests.Rendering;

public sealed class ProjectionTemplateContractsTests {
    [Fact]
    public void ProjectionTemplateContext_SuppliedInputs_ExposesThem() {
        RenderFragment defaultBody = static _ => { };
        ProjectionTemplateSectionRenderer sectionRenderer = static _ => static _ => { };
        ProjectionTemplateRowRenderer<string> rowRenderer = static _ => static _ => { };
        ProjectionTemplateFieldRenderer<string> fieldRenderer = static (_, _) => static _ => { };
        IReadOnlyList<string> items = ["a"];
        IReadOnlyList<ProjectionTemplateColumnDescriptor> columns = [new("Id", "Id", null, null)];
        IReadOnlyList<ProjectionTemplateSectionDescriptor> sections = [new("Body", "Body", "Body")];

        ProjectionTemplateContext<string> context = new(
            typeof(string), "Demo", ProjectionRole.StatusOverview, null, items, columns, sections,
            defaultBody, sectionRenderer, rowRenderer, fieldRenderer);

        context.ProjectionType.ShouldBe(typeof(string));
        context.BoundedContext.ShouldBe("Demo");
        context.Items.ShouldBeSameAs(items);
        context.Columns.ShouldBeSameAs(columns);
        context.Sections.ShouldBeSameAs(sections);
        context.DefaultBody.ShouldBeSameAs(defaultBody);
    }

    [Fact]
    public void ProjectionTemplateContext_NullRequiredInputs_Throws() {
        RenderFragment fragment = static _ => { };
        ProjectionTemplateSectionRenderer section = static _ => static _ => { };
        ProjectionTemplateRowRenderer<string> row = static _ => static _ => { };
        ProjectionTemplateFieldRenderer<string> field = static (_, _) => static _ => { };

        _ = Should.Throw<ArgumentNullException>(() => new ProjectionTemplateContext<string>(null!, null, null, null, [], [], [], fragment, section, row, field));
        _ = Should.Throw<ArgumentNullException>(() => new ProjectionTemplateContext<string>(typeof(string), null, null, null, null!, [], [], fragment, section, row, field));
        _ = Should.Throw<ArgumentNullException>(() => new ProjectionTemplateContext<string>(typeof(string), null, null, null, [], [], [], null!, section, row, field));
    }
}
