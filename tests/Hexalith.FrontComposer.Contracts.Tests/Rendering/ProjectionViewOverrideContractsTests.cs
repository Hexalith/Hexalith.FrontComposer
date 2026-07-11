using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Rendering;

public sealed class ProjectionViewOverrideContractsTests {
    [Fact]
    public void ProjectionViewOverrideContractVersion_Current_IsPackedVersion()
        => ProjectionViewOverrideContractVersion.Current.ShouldBe(
            (ProjectionViewOverrideContractVersion.Major * 1_000_000)
            + (ProjectionViewOverrideContractVersion.Minor * 1_000)
            + ProjectionViewOverrideContractVersion.Build);

    [Fact]
    public void ProjectionViewOverrideDescriptor_SuppliedMetadata_StoresIt() {
        ProjectionViewOverrideDescriptor descriptor = new(
            typeof(string), ProjectionRole.DetailRecord, typeof(int),
            ProjectionViewOverrideContractVersion.Current, "test");

        descriptor.ProjectionType.ShouldBe(typeof(string));
        descriptor.Role.ShouldBe(ProjectionRole.DetailRecord);
        descriptor.ComponentType.ShouldBe(typeof(int));
        descriptor.RegistrationSource.ShouldBe("test");
    }
}
