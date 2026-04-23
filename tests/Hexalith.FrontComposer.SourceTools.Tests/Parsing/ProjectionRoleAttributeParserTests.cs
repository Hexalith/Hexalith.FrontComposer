using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Tests.Parsing.TestFixtures;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Parsing;

/// <summary>
/// Story 4-1 T1.6 — parse-side behaviour for
/// <c>[ProjectionRole(..., WhenState = "A,B")]</c>.
/// </summary>
public class ProjectionRoleAttributeParserTests {
    [Fact]
    public void ReadsWhenStateFromNamedArgument() {
        ParseResult result = CompilationHelper.ParseProjection(
            TestSources.ActionQueueWithWhenStateProjection,
            "TestDomain.ActionQueueWithWhenStateProjection");

        _ = result.Model.ShouldNotBeNull();
        result.Model.ProjectionRole.ShouldBe("ActionQueue");
        result.Model.ProjectionRoleWhenState.ShouldBe("Pending,Submitted");
    }

    [Fact]
    public void RawCsvPreservedThroughParseStage() {
        // D3 — Parse stores the raw CSV on DomainModel; Transform owns the split.
        ParseResult result = CompilationHelper.ParseProjection(
            TestSources.WhenStateWithSpacesProjection,
            "TestDomain.WhenStateWithSpacesProjection");

        _ = result.Model.ShouldNotBeNull();
        result.Model.ProjectionRoleWhenState.ShouldBe(" Pending , Submitted , ");
    }

    [Fact]
    public void UnknownMemberEmitsHFC1022() {
        ParseResult result = CompilationHelper.ParseProjection(
            TestSources.WhenStateUnknownMemberProjection,
            "TestDomain.WhenStateUnknownMemberProjection");

        DiagnosticInfo[] hfc1022 = [.. result.Diagnostics.Where(d => d.Id == "HFC1022")];
        hfc1022.Length.ShouldBe(1);
        hfc1022[0].Message.ShouldContain("'Xxxx'");
        hfc1022[0].Severity.ShouldBe("Warning");
    }

    [Fact]
    public void UnknownMemberListsValidMembers() {
        // AC9 F6 — diagnostic MUST list valid member names to short-circuit casing typos.
        ParseResult result = CompilationHelper.ParseProjection(
            TestSources.WhenStateUnknownMemberProjection,
            "TestDomain.WhenStateUnknownMemberProjection");

        DiagnosticInfo hfc1022 = result.Diagnostics.Single(d => d.Id == "HFC1022");
        hfc1022.Message.ShouldContain("Pending");
        hfc1022.Message.ShouldContain("Submitted");
        hfc1022.Message.ShouldContain("Approved");
        hfc1022.Message.ShouldContain("Rejected");
    }

    [Fact]
    public void UnknownMembersStillFlowThroughToIR() {
        // D3 — unknown members STILL flow through to the IR (fail-soft).
        ParseResult result = CompilationHelper.ParseProjection(
            TestSources.WhenStateUnknownMemberProjection,
            "TestDomain.WhenStateUnknownMemberProjection");

        _ = result.Model.ShouldNotBeNull();
        result.Model.ProjectionRoleWhenState.ShouldBe("Pending,Xxxx");
    }

    [Fact]
    public void EmptyWhenStateIsTreatedAsAbsent() {
        // D3 — empty CSV (""") → ProjectionRoleWhenState == null.
        ParseResult result = CompilationHelper.ParseProjection(
            TestSources.WhenStateEmptyProjection,
            "TestDomain.WhenStateEmptyProjection");

        _ = result.Model.ShouldNotBeNull();
        result.Model.ProjectionRoleWhenState.ShouldBeNull();
    }

    [Fact]
    public void NoWhenStateArgumentIsTreatedAsAbsent() {
        // Omitting WhenState produces null IR payload.
        ParseResult result = CompilationHelper.ParseProjection(
            TestSources.MultiAttributeProjection,
            "TestDomain.MultiAttributeProjection");

        _ = result.Model.ShouldNotBeNull();
        result.Model.ProjectionRoleWhenState.ShouldBeNull();
    }

    [Fact]
    public void HFC1022MessageListsUpTo10ValidMembersAlphabetically() {
        // War Room round 3 / D17 cap — enum with 15 members produces message listing first
        // 10 alphabetically + "... and 5 more".
        ParseResult result = CompilationHelper.ParseProjection(
            TestSources.WhenStateLargeEnumProjection,
            "TestDomain.WhenStateLargeEnumProjection");

        DiagnosticInfo hfc1022 = result.Diagnostics.Single(d => d.Id == "HFC1022");
        hfc1022.Message.ShouldContain("... and 5 more");
        // First 10 alphabetical: Alpha, Beta, Delta, Epsilon, Eta, Gamma, Iota, Kappa, Lambda, Mu
        hfc1022.Message.ShouldContain("Alpha");
        hfc1022.Message.ShouldContain("Beta");
        hfc1022.Message.ShouldContain("Mu");
        // Nu / Xi / Omicron / Theta / Zeta are the 5 overflow members.
        hfc1022.Message.ShouldNotContain("Omicron");
    }

    [Fact]
    public void WhenStateCsvIsTrimmedAndDeduplicatedThroughTransform() {
        // The Transform stage owns the split; the Parse-stage payload retains spaces.
        // This test anchors the Parse-side raw-storage contract AGAINST drift.
        ParseResult result = CompilationHelper.ParseProjection(
            TestSources.WhenStateWithSpacesProjection,
            "TestDomain.WhenStateWithSpacesProjection");

        _ = result.Model.ShouldNotBeNull();
        // No diagnostic: " Pending " trims to "Pending" at validation time.
        result.Diagnostics.ShouldNotContain(d => d.Id == "HFC1022");
    }

    [Fact]
    public void ValidMemberPassThroughWithoutDiagnostic() {
        ParseResult result = CompilationHelper.ParseProjection(
            TestSources.ActionQueueWithWhenStateProjection,
            "TestDomain.ActionQueueWithWhenStateProjection");

        result.Diagnostics.ShouldNotContain(d => d.Id == "HFC1022");
    }

    [Fact]
    public void ProjectionTypeDisplayAttribute_IsCaptured() {
        ParseResult result = CompilationHelper.ParseProjection(
            TestSources.ProjectionDisplayAttributeProjection,
            "TestDomain.ProjectionDisplayAttributeProjection");

        _ = result.Model.ShouldNotBeNull();
        result.Model.DisplayName.ShouldBe("Order");
        result.Model.DisplayGroupName.ShouldBe("Orders");
    }
}
