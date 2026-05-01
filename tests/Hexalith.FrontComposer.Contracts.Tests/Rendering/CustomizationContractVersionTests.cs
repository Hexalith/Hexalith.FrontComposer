using Hexalith.FrontComposer.Contracts.Rendering;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Rendering;

/// <summary>
/// Story 6-6 T2: shared packed-version comparison rules for customization contracts.
/// </summary>
public sealed class CustomizationContractVersionTests {
    [Theory]
    [InlineData(1_000_000, 1, 0, 0)]
    [InlineData(2_003_004, 2, 3, 4)]
    public void Unpack_ReturnsVersionTriplet(int packed, int major, int minor, int build) {
        CustomizationContractVersion.Unpack(packed).ShouldBe(new CustomizationContractVersion(major, minor, build));
    }

    [Fact]
    public void Compare_ExactMatch_SelectsOverride() {
        CustomizationContractVersionComparison comparison =
            CustomizationContractVersion.Compare(expectedPacked: 1_000_000, actualPacked: 1_000_000);

        comparison.Decision.ShouldBe(CustomizationContractVersionDecision.Exact);
        comparison.CanSelect.ShouldBeTrue();
        comparison.ShouldReportDiagnostic.ShouldBeFalse();
    }

    [Fact]
    public void Compare_MajorMismatch_SuppressesSelection() {
        CustomizationContractVersionComparison comparison =
            CustomizationContractVersion.Compare(expectedPacked: 2_000_000, actualPacked: 1_000_000);

        comparison.Decision.ShouldBe(CustomizationContractVersionDecision.MajorMismatch);
        comparison.CanSelect.ShouldBeFalse();
        comparison.ShouldReportDiagnostic.ShouldBeTrue();
    }

    [Fact]
    public void Compare_MinorDrift_WarnsAndSelects() {
        CustomizationContractVersionComparison comparison =
            CustomizationContractVersion.Compare(expectedPacked: 1_001_000, actualPacked: 1_000_000);

        comparison.Decision.ShouldBe(CustomizationContractVersionDecision.MinorDrift);
        comparison.CanSelect.ShouldBeTrue();
        comparison.ShouldReportDiagnostic.ShouldBeTrue();
    }

    [Fact]
    public void Compare_BuildOnlyDrift_SelectsWithoutDiagnostic() {
        CustomizationContractVersionComparison comparison =
            CustomizationContractVersion.Compare(expectedPacked: 1_000_007, actualPacked: 1_000_000);

        comparison.Decision.ShouldBe(CustomizationContractVersionDecision.BuildDrift);
        comparison.CanSelect.ShouldBeTrue();
        comparison.ShouldReportDiagnostic.ShouldBeFalse();
    }
}
