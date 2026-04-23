using Hexalith.FrontComposer.Contracts.Attributes;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Attributes;

/// <summary>
/// Story 4-1 T3.11 / AC7 — enum-cap gate. PRD FR4 caps the enum at 5 and names each
/// member. Dashboard stays in v1 (reserved, deferred to Story 6-3 per D16/AC10 — not
/// removed).
/// </summary>
public class ProjectionRoleEnumTests {
    [Fact]
    public void EnumMembersMatchPrdFR4CapOf5() {
        string[] names = Enum.GetNames<ProjectionRole>();
        names.Length.ShouldBe(5);
        names.ShouldContain("ActionQueue");
        names.ShouldContain("StatusOverview");
        names.ShouldContain("DetailRecord");
        names.ShouldContain("Timeline");
        names.ShouldContain("Dashboard");
    }

    [Fact]
    public void DashboardIsStillPartOfEnum() {
        // Story 4-1 D16 / AC10 — Dashboard is reserved, not broken. Transform emits
        // HFC1023 Information; render falls back to Default. A future Story 6-3 upgrade
        // activates full Dashboard rendering without changing the enum shape.
        Enum.IsDefined(typeof(ProjectionRole), ProjectionRole.Dashboard).ShouldBeTrue();
    }
}
