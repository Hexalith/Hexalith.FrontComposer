using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.SourceTools.Diagnostics;

using Microsoft.CodeAnalysis;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Diagnostics;

/// <summary>
/// Story 6-5 T7 / Diagnostic Oracle Table — HFC1047/1048/1049 are dev-mode diagnostic IDs
/// reserved for invalid annotation site, unsupported emission level, and contract version drift.
/// HFC2010 is the defensive Shell-side runtime log for activation outside Development.
/// </summary>
public class Hfc1047To1049DevModeReservationTests {
    [Fact]
    public void HFC1047DescriptorIsRegistered() {
        DiagnosticDescriptor descriptor = DiagnosticDescriptors.DevModeAnnotationSiteInvalid;

        descriptor.Id.ShouldBe("HFC1047");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Info);
        descriptor.Category.ShouldBe("HexalithFrontComposer");
        descriptor.IsEnabledByDefault.ShouldBeTrue();
        descriptor.Title.ToString().ShouldContain("annotation site", Case.Insensitive);
    }

    [Fact]
    public void HFC1048DescriptorIsRegistered() {
        DiagnosticDescriptor descriptor = DiagnosticDescriptors.DevModeUnsupportedEmissionLevel;

        descriptor.Id.ShouldBe("HFC1048");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Info);
        descriptor.Category.ShouldBe("HexalithFrontComposer");
        descriptor.IsEnabledByDefault.ShouldBeTrue();
        descriptor.Title.ToString().ShouldContain("emission level", Case.Insensitive);
    }

    [Fact]
    public void HFC1049DescriptorIsRegistered() {
        DiagnosticDescriptor descriptor = DiagnosticDescriptors.DevModeContractVersionDrift;

        descriptor.Id.ShouldBe("HFC1049");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Info);
        descriptor.Category.ShouldBe("HexalithFrontComposer");
        descriptor.IsEnabledByDefault.ShouldBeTrue();
        descriptor.Title.ToString().ShouldContain("stale", Case.Insensitive);
    }

    [Fact]
    public void FcDiagnosticIdsPublishesDevModeConstants() {
        FcDiagnosticIds.HFC1047_DevModeAnnotationSiteInvalid.ShouldBe("HFC1047");
        FcDiagnosticIds.HFC1048_DevModeUnsupportedEmissionLevel.ShouldBe("HFC1048");
        FcDiagnosticIds.HFC1049_DevModeContractVersionDrift.ShouldBe("HFC1049");
        FcDiagnosticIds.HFC2010_DevModeActivationOutsideDevelopment.ShouldBe("HFC2010");
    }

    [Fact]
    public void DevModeDescriptorIdsAreContiguousAndUnique() {
        string[] ids = [
            DiagnosticDescriptors.DevModeAnnotationSiteInvalid.Id,
            DiagnosticDescriptors.DevModeUnsupportedEmissionLevel.Id,
            DiagnosticDescriptors.DevModeContractVersionDrift.Id,
        ];

        ids.ShouldBeUnique();
        ids.ShouldBe(["HFC1047", "HFC1048", "HFC1049"]);
    }
}
