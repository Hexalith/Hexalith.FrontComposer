using Hexalith.FrontComposer.Contracts.DevMode;
using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services.Customization;
using Hexalith.FrontComposer.Shell.Services.Diagnostics;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Diagnostics;

public sealed class CustomizationContractMismatchDiagnosticProviderTests {
    [Fact]
    public void GetDiagnostics_ConvertsRejectionToSanitizedCustomizationDiagnostic() {
        CustomizationContractRejectionLog log = new();
        log.Record(NewRejection());
        CustomizationContractMismatchDiagnosticProvider sut = new(log);

        CustomizationDiagnostic diagnostic = sut.GetDiagnostics().ShouldHaveSingleItem();

        diagnostic.Id.ShouldBe(FcDiagnosticIds.HFC1041_ProjectionSlotContractVersionMismatch);
        diagnostic.Phase.ShouldBe(CustomizationDiagnosticPhase.Runtime);
        diagnostic.Level.ShouldBe(CustomizationLevel.Level3);
        diagnostic.ProjectionTypeName.ShouldBe("Demo.CounterProjection");
        diagnostic.ComponentTypeName.ShouldBe("Demo.CounterSlot");
        diagnostic.Role.ShouldBe("DetailRecord");
        diagnostic.FieldName.ShouldBe("Count");
        diagnostic.Expected.ShouldContain("2.0.0");
        diagnostic.Got.ShouldContain("1.0.0");
        diagnostic.Got.ShouldContain("MajorMismatch");
        diagnostic.DocsLink.ShouldBe("https://hexalith.github.io/FrontComposer/diagnostics/HFC1041");
        diagnostic.Properties["expectedVersion"].ShouldBe("2.0.0");
        diagnostic.Properties["actualVersion"].ShouldBe("1.0.0");
        diagnostic.Properties.ShouldNotContainKey("tenantId");
        CustomizationDiagnosticFormatter.Format(diagnostic).ShouldContain("DocsLink:");
    }

    internal static CustomizationContractRejection NewRejection()
        => new(
            CustomizationLevel.Level3,
            ProjectionTypeName: "Demo.CounterProjection",
            ComponentTypeName: "Demo.CounterSlot",
            Role: "DetailRecord",
            FieldName: "Count",
            Comparison: new CustomizationContractVersionComparison(
                Expected: new CustomizationContractVersion(2, 0, 0),
                Actual: new CustomizationContractVersion(1, 0, 0),
                Decision: CustomizationContractVersionDecision.MajorMismatch,
                CanSelect: false,
                ShouldReportDiagnostic: true),
            DiagnosticId: FcDiagnosticIds.HFC1041_ProjectionSlotContractVersionMismatch);
}
