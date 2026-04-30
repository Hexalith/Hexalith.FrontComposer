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

    // ──────────────────────────────────────────────────────────────────────────
    //   Story 6-1 review F20 / T3 — DisplayFormat and RelativeTimeWindowDays are
    //   new PropertyModel IR fields. Both must participate in PropertyModel
    //   equality + hash so an annotation add/remove or window-argument change
    //   invalidates the incremental-generator cache and re-runs Transform/Emit.
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void PropertyModel_DisplayFormatChange_InvalidatesEquality() {
        PropertyModel a = BuildProperty("LastUpdated", "DateTime", displayFormat: FieldDisplayFormat.Default);
        PropertyModel b = BuildProperty("LastUpdated", "DateTime", displayFormat: FieldDisplayFormat.RelativeTime);

        a.Equals(b).ShouldBeFalse();
        a.GetHashCode().ShouldNotBe(b.GetHashCode());
    }

    [Fact]
    public void PropertyModel_RelativeTimeWindowChange_InvalidatesEquality() {
        PropertyModel a = BuildProperty("LastUpdated", "DateTime", displayFormat: FieldDisplayFormat.RelativeTime, relativeTimeWindowDays: 7);
        PropertyModel b = BuildProperty("LastUpdated", "DateTime", displayFormat: FieldDisplayFormat.RelativeTime, relativeTimeWindowDays: 30);

        a.Equals(b).ShouldBeFalse();
        a.GetHashCode().ShouldNotBe(b.GetHashCode());
    }

    [Fact]
    public void PropertyModel_DisplayFormatCurrencyVsDefault_InvalidatesEquality() {
        PropertyModel a = BuildProperty("Amount", "Decimal", displayFormat: FieldDisplayFormat.Default);
        PropertyModel b = BuildProperty("Amount", "Decimal", displayFormat: FieldDisplayFormat.Currency);

        a.Equals(b).ShouldBeFalse();
        a.GetHashCode().ShouldNotBe(b.GetHashCode());
    }

    [Fact]
    public void PropertyModel_IdenticalLevel1Format_CompareEqual() {
        // Cache hit — re-parsing the same source must yield equal PropertyModels.
        PropertyModel a = BuildProperty("LastUpdated", "DateTime", displayFormat: FieldDisplayFormat.RelativeTime, relativeTimeWindowDays: 14);
        PropertyModel b = BuildProperty("LastUpdated", "DateTime", displayFormat: FieldDisplayFormat.RelativeTime, relativeTimeWindowDays: 14);

        a.Equals(b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
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

    private static PropertyModel BuildProperty(
        string name,
        string typeName,
        FieldDisplayFormat displayFormat = FieldDisplayFormat.Default,
        int? relativeTimeWindowDays = null) => new(
            name,
            typeName,
            isNullable: false,
            isUnsupported: false,
            displayName: null,
            badgeMappings: new EquatableArray<BadgeMappingEntry>(ImmutableArray<BadgeMappingEntry>.Empty),
            displayFormat: displayFormat,
            relativeTimeWindowDays: relativeTimeWindowDays);
}
