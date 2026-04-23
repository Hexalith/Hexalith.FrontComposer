using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Transforms;

/// <summary>
/// Story 4-1 T2.6 — <see cref="RazorModelTransform"/> role → strategy mapping +
/// <c>WhenStates</c> CSV split + HFC1023 Dashboard dispatch + HFC1024 unknown-value
/// fallback. Full Exhaustiveness discipline per ADR-052.
/// </summary>
public class RazorModelRoleStrategyTests {
    [Theory]
    [InlineData("ActionQueue", ProjectionRenderStrategy.ActionQueue)]
    [InlineData("StatusOverview", ProjectionRenderStrategy.StatusOverview)]
    [InlineData("DetailRecord", ProjectionRenderStrategy.DetailRecord)]
    [InlineData("Timeline", ProjectionRenderStrategy.Timeline)]
    [InlineData("Dashboard", ProjectionRenderStrategy.Dashboard)]
    public void MapsAllKnownRoleNames(string role, ProjectionRenderStrategy expected) {
        DomainModel model = BuildDomainModel(role);

        RazorModel razor = RazorModelTransform.Transform(model);

        razor.Strategy.ShouldBe(expected);
    }

    [Fact]
    public void NullDomainRoleMapsToDefault() {
        DomainModel model = BuildDomainModel(null);

        RazorModel razor = RazorModelTransform.Transform(model);

        razor.Strategy.ShouldBe(ProjectionRenderStrategy.Default);
    }

    [Fact]
    public void EmptyDomainRoleMapsToDefault() {
        DomainModel model = BuildDomainModel(string.Empty);

        RazorModel razor = RazorModelTransform.Transform(model);

        razor.Strategy.ShouldBe(ProjectionRenderStrategy.Default);
    }

    [Fact]
    public void UnknownRoleValueMapsToDefault() {
        // D15 — if Parse ever delivered a non-enum role string (or a numeric payload
        // following an unsafe cast), Transform falls back to Default. HFC1024 already
        // emitted at Parse.
        DomainModel model = BuildDomainModel("999");

        RazorModel razor = RazorModelTransform.Transform(model);

        razor.Strategy.ShouldBe(ProjectionRenderStrategy.Default);
    }

    [Fact]
    public void WhenStateCsvIsSplitAndTrimmed() {
        DomainModel model = BuildDomainModel("ActionQueue", whenState: " Pending , Submitted , ");

        RazorModel razor = RazorModelTransform.Transform(model);

        string[] whenStates = [.. razor.WhenStates];
        whenStates.ShouldBe(["Pending", "Submitted"]);
    }

    [Fact]
    public void EmptyWhenStateYieldsEmptyArray() {
        DomainModel model = BuildDomainModel("ActionQueue", whenState: null);

        RazorModel razor = RazorModelTransform.Transform(model);

        razor.WhenStates.Count.ShouldBe(0);
    }

    [Fact]
    public void DashboardRoleEmitsHFC1023() {
        // D16 — HFC1023 Information emitted once per Dashboard projection per compilation.
        DomainModel model = BuildDomainModel("Dashboard");
        List<DiagnosticInfo> diagnostics = [];

        _ = RazorModelTransform.Transform(model, diagnostics);

        DiagnosticInfo[] hfc1023 = [.. diagnostics.Where(d => d.Id == "HFC1023")];
        hfc1023.Length.ShouldBe(1);
        hfc1023[0].Severity.ShouldBe("Info");
        hfc1023[0].Message.ShouldContain("Test");
    }

    [Fact]
    public void NonDashboardRolesDoNotEmitHFC1023() {
        DomainModel actionQueue = BuildDomainModel("ActionQueue");
        DomainModel defaultRole = BuildDomainModel(null);
        List<DiagnosticInfo> diagnostics = [];

        _ = RazorModelTransform.Transform(actionQueue, diagnostics);
        _ = RazorModelTransform.Transform(defaultRole, diagnostics);

        diagnostics.ShouldNotContain(d => d.Id == "HFC1023");
    }

    [Fact]
    public void WhenStatesAndStrategyParticipateInEquality() {
        // T1.7 cache-parity cornerstone — editing WhenState / role produces a DIFFERENT
        // RazorModel so the incremental-generator cache invalidates.
        RazorModel a = RazorModelTransform.Transform(BuildDomainModel("ActionQueue", whenState: "Pending"));
        RazorModel b = RazorModelTransform.Transform(BuildDomainModel("ActionQueue", whenState: "Pending,Submitted"));
        RazorModel c = RazorModelTransform.Transform(BuildDomainModel("Timeline", whenState: "Pending"));

        a.Equals(b).ShouldBeFalse();
        a.Equals(c).ShouldBeFalse();
        a.Equals(RazorModelTransform.Transform(BuildDomainModel("ActionQueue", whenState: "Pending")))
            .ShouldBeTrue();
    }

    [Fact]
    public void TwoIndependentTransformsOfIdenticalSourceProduceEqualModels() {
        // Round 4 per Winston — guards EquatableArray<T>.Equals against reference-equality
        // regression. Two independent transforms of identical input must yield equal models
        // AND equal hash codes.
        DomainModel domain = BuildDomainModel("ActionQueue", whenState: "Pending,Submitted");

        RazorModel m1 = RazorModelTransform.Transform(domain);
        RazorModel m2 = RazorModelTransform.Transform(domain);

        m1.Equals(m2).ShouldBeTrue();
        m1.GetHashCode().ShouldBe(m2.GetHashCode());
    }

    private static DomainModel BuildDomainModel(string? role, string? whenState = null) {
        return new DomainModel(
            "Test",
            "TestDomain",
            null,
            null,
            role,
            new EquatableArray<PropertyModel>(ImmutableArray<PropertyModel>.Empty),
            whenState);
    }
}
