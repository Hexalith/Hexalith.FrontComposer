using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Rendering;

public sealed class ProjectionTemplateContractsTests {
    [Fact]
    public void ProjectionTemplateContractVersion_Current_PacksMajorMinorBuild() {
        ProjectionTemplateContractVersion.Current.ShouldBe(1_000_000);
        ProjectionTemplateContractVersion.Major.ShouldBe(1);
        ProjectionTemplateContractVersion.Minor.ShouldBe(0);
        ProjectionTemplateContractVersion.Build.ShouldBe(0);
    }

    [Fact]
    public void ProjectionTemplateAttribute_SuppliedInputs_StoresThem() {
        ProjectionTemplateAttribute attribute = new(typeof(string), ProjectionTemplateContractVersion.Current);

        attribute.ProjectionType.ShouldBe(typeof(string));
        attribute.ExpectedContractVersion.ShouldBe(ProjectionTemplateContractVersion.Current);
        attribute.Role.ShouldBe(ProjectionRole.ActionQueue);
    }

    [Fact]
    public void ProjectionTemplateDescriptor_SameValues_IsStructurallyEqual() {
        ProjectionTemplateDescriptor descriptor = new(typeof(string), null, typeof(int), ProjectionTemplateContractVersion.Current);
        ProjectionTemplateDescriptor copy = new(typeof(string), null, typeof(int), ProjectionTemplateContractVersion.Current);

        descriptor.ShouldBe(copy);
    }
}
