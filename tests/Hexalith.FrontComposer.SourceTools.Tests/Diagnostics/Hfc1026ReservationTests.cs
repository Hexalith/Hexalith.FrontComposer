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
}
