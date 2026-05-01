using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.SourceTools.Diagnostics;

using Microsoft.CodeAnalysis;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Diagnostics;

/// <summary>
/// Story 4-2 T3.3 / D7 — HFC1026 is a reserved diagnostic with no call site in 4-2. Holds
/// the id so Story 10-2's specimen checker can consume it without reopening the diagnostic
/// table.
/// </summary>
public class Hfc1026ReservationTests {
    [Fact]
    public void HFC1026DescriptorIsRegistered() {
        DiagnosticDescriptor descriptor = DiagnosticDescriptors.ColorOnlyBadgeDetected;

        descriptor.Id.ShouldBe("HFC1026");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Warning);
        descriptor.Category.ShouldBe("HexalithFrontComposer");
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    public void FcDiagnosticIdsPublishesHFC1026Constant() {
        FcDiagnosticIds.HFC1026_ColorOnlyBadgeDetected.ShouldBe("HFC1026");
    }

    [Fact]
    public void FcDiagnosticIdsPublishesHFC1025Constant() {
        FcDiagnosticIds.HFC1025_BadgeSlotFallbackApplied.ShouldBe("HFC1025");
    }

    [Fact]
    public void FcDiagnosticIdsPublishesLevel3SlotConstants() {
        FcDiagnosticIds.HFC1038_ProjectionSlotSelectorInvalid.ShouldBe("HFC1038");
        FcDiagnosticIds.HFC1039_ProjectionSlotComponentInvalid.ShouldBe("HFC1039");
        FcDiagnosticIds.HFC1040_ProjectionSlotDuplicate.ShouldBe("HFC1040");
        FcDiagnosticIds.HFC1041_ProjectionSlotContractVersionMismatch.ShouldBe("HFC1041");
    }

    [Fact]
    public void FcDiagnosticIdsPublishesLevel4ViewOverrideConstants() {
        FcDiagnosticIds.HFC1042_ProjectionViewOverrideInvalidProjectionType.ShouldBe("HFC1042");
        FcDiagnosticIds.HFC1043_ProjectionViewOverrideComponentInvalid.ShouldBe("HFC1043");
        FcDiagnosticIds.HFC1044_ProjectionViewOverrideDuplicate.ShouldBe("HFC1044");
        FcDiagnosticIds.HFC1045_ProjectionViewOverrideContractVersionMismatch.ShouldBe("HFC1045");
        FcDiagnosticIds.HFC1046_ProjectionViewOverrideAccessibilityWarning.ShouldBe("HFC1046");
        FcDiagnosticIds.HFC2121_ProjectionViewOverrideRenderFault.ShouldBe("HFC2121");
    }
}
