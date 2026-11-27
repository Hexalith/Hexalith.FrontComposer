using Hexalith.FrontComposer.Contracts.Rendering;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Rendering;

public sealed class ProjectionSlotContractsTests {
    [Fact]
    public void ProjectionSlotContractVersion_Current_PacksMajorMinorBuild() {
        ProjectionSlotContractVersion.Current.ShouldBe(1_000_000);
        ProjectionSlotContractVersion.Current.ShouldBe(
            (ProjectionSlotContractVersion.Major * 1_000_000)
            + (ProjectionSlotContractVersion.Minor * 1_000)
            + ProjectionSlotContractVersion.Build);
    }

    [Fact]
    public void ProjectionSlotDescriptor_StructuralEqualityAndValidation() {
        ProjectionSlotDescriptor descriptor = new(typeof(Projection), "Priority", typeof(int), null, typeof(string), ProjectionSlotContractVersion.Current);
        ProjectionSlotDescriptor copy = new(typeof(Projection), "Priority", typeof(int), null, typeof(string), ProjectionSlotContractVersion.Current);

        descriptor.ShouldBe(copy);
        _ = Should.Throw<ArgumentException>(() => new ProjectionSlotDescriptor(typeof(Projection), " ", typeof(int), null, typeof(string), 1));
    }

    [Fact]
    public void ProjectionSlotSelector_DirectProperty_ReturnsIdentity()
        => ProjectionSlotSelector.Parse<Projection, int>(projection => projection.Priority)
            .ShouldBe(new ProjectionSlotFieldIdentity("Priority", typeof(int)));

    [Fact]
    public void InvalidProjectionSlotSelectorPublishesCanonicalHelpLink()
    {
        ProjectionSlotSelectorException exception = Should.Throw<ProjectionSlotSelectorException>(() =>
            ProjectionSlotSelector.Parse<Projection, int>(projection => projection.Priority + 1));

        const string expectedLink = "https://hexalith.github.io/FrontComposer/diagnostics/HFC1038";
        exception.HelpLink.ShouldBe(expectedLink);
        exception.Message.ShouldContain(expectedLink);
        exception.Message.ShouldStartWith("HFC1038");
    }

    public sealed record Projection(int Priority);
}
