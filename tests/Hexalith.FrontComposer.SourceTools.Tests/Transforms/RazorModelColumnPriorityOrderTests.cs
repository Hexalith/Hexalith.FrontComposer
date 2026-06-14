using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Transforms;

/// <summary>
/// Story 2.5 AC2 — pins the Transform-stage column ORDER produced by the
/// <c>(Priority ?? int.MaxValue, declarationOrder)</c> stable sort in
/// <see cref="RazorModelTransform"/>. The pre-existing emitter pin
/// (<c>RazorEmitterColumnPrioritizerTests.DescriptorListContainsAllColumnKeys</c>) asserts every
/// key is PRESENT, not that the descriptor list is ORDERED, and
/// <see cref="RazorModelTransformTests"/> carries no priority test — so a regression that dropped
/// the index-stable tiebreaker or stopped gating on <c>anyPriority</c> would pass every existing
/// test. These pins close that gap by asserting the resulting <see cref="ColumnModel"/> order
/// directly at the transform level (isolated from the &gt;15-col prioritizer wrap).
/// </summary>
[Trait("Category", "MutationErrorHandling")]
public class RazorModelColumnPriorityOrderTests {
    private static readonly EquatableArray<BadgeMappingEntry> _emptyBadges = new(ImmutableArray<BadgeMappingEntry>.Empty);

    [Fact]
    public void Transform_MixedPrioritiesAndUnannotated_AnnotatedAscendThenUnannotatedTrailInDeclarationOrder() {
        // Declared B(prio 2), A(none), C(prio 1) → expect C, B, A:
        // annotated ascend by priority (C=1, B=2); unannotated (A) trails via the int.MaxValue
        // sentinel, keeping declaration order among unannotated.
        RazorModel result = RazorModelTransform.Transform(Model(
            Prop("B", priority: 2),
            Prop("A"),
            Prop("C", priority: 1)));

        result.Columns.Count.ShouldBe(3);
        result.Columns[0].PropertyName.ShouldBe("C");
        result.Columns[1].PropertyName.ShouldBe("B");
        result.Columns[2].PropertyName.ShouldBe("A");
    }

    [Fact]
    public void Transform_MultipleUnannotatedTrailing_KeepDeclarationOrderAmongThemselves() {
        // High(prio 5), then three unannotated U1, U2, U3 → High first, then U1, U2, U3 in
        // declaration order (all share the int.MaxValue sentinel; the index tiebreaker holds).
        RazorModel result = RazorModelTransform.Transform(Model(
            Prop("High", priority: 5),
            Prop("U1"),
            Prop("U2"),
            Prop("U3")));

        result.Columns.Count.ShouldBe(4);
        result.Columns[0].PropertyName.ShouldBe("High");
        result.Columns[1].PropertyName.ShouldBe("U1");
        result.Columns[2].PropertyName.ShouldBe("U2");
        result.Columns[3].PropertyName.ShouldBe("U3");
    }

    [Fact]
    public void Transform_NoColumnDeclaresPriority_PreservesDeclarationOrderByteForByte() {
        // NO-OP gate: when NOT a single column declares a priority, the sort is skipped entirely
        // and declaration order is preserved verbatim — the CounterProjectionApprovalTests
        // invariant. Asserted explicitly so a regression that ALWAYS sorts is caught here.
        RazorModel result = RazorModelTransform.Transform(Model(
            Prop("Zebra"),
            Prop("Apple"),
            Prop("Mango")));

        result.Columns.Count.ShouldBe(3);
        result.Columns[0].PropertyName.ShouldBe("Zebra");
        result.Columns[1].PropertyName.ShouldBe("Apple");
        result.Columns[2].PropertyName.ShouldBe("Mango");

        // And the carried priorities are all null (no sentinel materialised onto the model).
        result.Columns[0].Priority.ShouldBeNull();
        result.Columns[1].Priority.ShouldBeNull();
        result.Columns[2].Priority.ShouldBeNull();
    }

    [Fact]
    public void Transform_TwoColumnsShareSamePriority_KeepDeclarationOrderViaIndexTiebreaker() {
        // Stability on collision: First(prio 1) and Second(prio 1) share the explicit value;
        // the index tiebreaker keeps their declaration order. Last(prio 2) sorts after both.
        RazorModel result = RazorModelTransform.Transform(Model(
            Prop("First", priority: 1),
            Prop("Last", priority: 2),
            Prop("Second", priority: 1)));

        result.Columns.Count.ShouldBe(3);
        result.Columns[0].PropertyName.ShouldBe("First");
        result.Columns[1].PropertyName.ShouldBe("Second");
        result.Columns[2].PropertyName.ShouldBe("Last");
    }

    [Fact]
    public void Transform_SignedEdgePriorities_MinValuePinsToFront_MaxValueSortsAmongUnannotated() {
        // int.MinValue pins to the very front; an explicit int.MaxValue is numerically equal to
        // the unannotated sentinel, so it interleaves with unannotated columns by declaration
        // order (the tiebreaker), proving the sort is a plain signed-int compare with no special
        // casing beyond the null→MaxValue projection.
        RazorModel result = RazorModelTransform.Transform(Model(
            Prop("Unannotated"),
            Prop("Front", priority: int.MinValue),
            Prop("Back", priority: int.MaxValue),
            Prop("Mid", priority: 0)));

        result.Columns.Count.ShouldBe(4);
        result.Columns[0].PropertyName.ShouldBe("Front"); // int.MinValue
        result.Columns[1].PropertyName.ShouldBe("Mid");   // 0
        // "Unannotated" (sentinel MaxValue, index 0) precedes "Back" (explicit MaxValue, index 2).
        result.Columns[2].PropertyName.ShouldBe("Unannotated");
        result.Columns[3].PropertyName.ShouldBe("Back");
    }

    private static DomainModel Model(params PropertyModel[] props)
        => new("TestProjection", "TestDomain", "Test", null, null, new EquatableArray<PropertyModel>(props.ToImmutableArray()));

    private static PropertyModel Prop(string name, int? priority = null)
        => new(name, "String", isNullable: false, isUnsupported: false, displayName: null, badgeMappings: _emptyBadges, columnPriority: priority);
}
