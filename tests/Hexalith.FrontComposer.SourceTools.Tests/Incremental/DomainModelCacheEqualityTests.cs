using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Parsing;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Incremental;

/// <summary>
/// Story 4-1 T1.7 — DomainModel must participate in <see cref="IEquatable{T}"/> value-equality
/// for Roslyn incremental-generator cache correctness (ADR-006). Editing <c>WhenState</c>
/// between builds MUST produce a non-equal <c>DomainModel</c> so Parse → Transform → Emit
/// re-run.
/// </summary>
public class DomainModelCacheEqualityTests {
    [Fact]
    public void WhenStateFieldIsPartOfEquality() {
        DomainModel a = BuildModel(whenState: "Pending");
        DomainModel b = BuildModel(whenState: "Pending,Submitted");

        a.Equals(b).ShouldBeFalse();
        a.GetHashCode().ShouldNotBe(b.GetHashCode());
    }

    [Fact]
    public void NullVsEmptyWhenStateCompareEqual() {
        // Parse normalises empty-string WhenState to null, so two independently-parsed
        // DomainModels of identical source must be equal.
        DomainModel a = BuildModel(whenState: null);
        DomainModel b = BuildModel(whenState: null);

        a.Equals(b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void RoleChangeIsCacheInvalidating() {
        DomainModel a = BuildModel(role: "ActionQueue");
        DomainModel b = BuildModel(role: "Timeline");

        a.Equals(b).ShouldBeFalse();
    }

    [Fact]
    public void TwoIndependentParsesOfIdenticalSourceProduceEqualModels() {
        // Round 4 per Winston — guards EquatableArray<T>.Equals against reference-equality
        // regression at the Parse stage.
        DomainModel a = BuildModel(whenState: "Pending,Submitted");
        DomainModel b = BuildModel(whenState: "Pending,Submitted");

        a.Equals(b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void PreExistingFieldsStillParticipate() {
        // Regression anchor — the ProjectionRoleWhenState addition must not clobber the
        // older equality fields (TypeName / Namespace / BoundedContext / ProjectionRole).
        DomainModel a = BuildModel(typeName: "A");
        DomainModel b = BuildModel(typeName: "B");

        a.Equals(b).ShouldBeFalse();
    }

    private static DomainModel BuildModel(
        string typeName = "Test",
        string? role = "ActionQueue",
        string? whenState = null) => new(
            typeName,
            "TestDomain",
            boundedContext: "Ctx",
            boundedContextDisplayLabel: null,
            projectionRole: role,
            properties: new EquatableArray<PropertyModel>(ImmutableArray<PropertyModel>.Empty),
            projectionRoleWhenState: whenState);
}
